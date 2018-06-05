﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dispatcher.Database
{
	class Connection
	{
		readonly public static Connection X = new Connection();

		public bool Connected { get; private set; } = false;

		public MongoClient FClient;
		public IMongoDatabase FDatabase;

		private Connection()
		{

		}

		public void Connect()
		{
			try
			{
				//Connect to the server
				this.FClient = new MongoClient("mongodb://10.0.0.49");
				this.FDatabase = this.FClient.GetDatabase("Dispatcher");

				//Check if connected
				{
					this.Connected = (bool)this.FDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
				}

				//Serialize enums as strings
				{
					ConventionRegistry.Register("EnumStringConvention"
						, new ConventionPack {
							new EnumRepresentationConvention(BsonType.String)
						}
					, t => true);
				}

				if (this.Connected)
				{
					Logger.Log<Connection>(Logger.Level.Trace, "--------------- SESION START : Database connected");
				}
			}
			catch (Exception e)
			{
				Logger.Log<Connection>(Logger.Level.Warning, "Failed to connect to MongoDB for DataLogging : " + e.Message);
				this.Connected = false;
				Logger.Log<Connection>(Logger.Level.Trace, "Database not connected for logging. No MongoDB server found");
			}
		}

		public IMongoCollection<T> GetCollection<T>()
		{
			if (!this.Connected)
			{
				throw (new Exception("No database connection"));
			}

			var dataRowAttribute = Attribute.GetCustomAttribute(typeof(T), typeof(DataRowAttribute)) as DataRowAttribute;
			return this.FDatabase.GetCollection<T>(dataRowAttribute.Collection);
		}

		public void Log<T>(T data)
		{
			if (this.Connected)
			{
				var collection = this.GetCollection<T>();
				collection.InsertOne(data);
			}
		}

		public IMongoDatabase Database
		{
			get
			{
				return this.FDatabase;
			}
		}
	}
}
