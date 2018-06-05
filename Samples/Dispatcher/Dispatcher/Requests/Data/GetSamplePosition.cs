using DynamixelSDKSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Attributes;

namespace Dispatcher.Requests.Data
{
    [Serializable]
    [RequestHandler(Method = Method.POST)]
    class GetSamplePosition : IRequest
    {
        public int servoID { get; set; }
        private int lposition;
        private DateTime ltime;
        private int eposition;
        private DateTime etime;
        public string timeString { get; set; }
        public DateTime time { get; set; }
        public double timeWindow { get; set; } = 60;

        //var collection =  database.GetCollection<TDocument>("collectionname");
        public object Perform()
        {
            var collection = Database.Connection.X.GetCollection<Database.Registers>();
            this.time = DateTime.Parse(timeString).ToUniversalTime();
            DateTime minTime = this.time - TimeSpan.FromSeconds(this.timeWindow / 2);
            DateTime maxTime = this.time + TimeSpan.FromSeconds(this.timeWindow / 2);
            
            var later_documents = from document in collection.AsQueryable<Database.Registers>()
                                  where document.ServoID == servoID
                                  where document.TimeStamp >= this.time
                                  where document.TimeStamp <= maxTime
                                  orderby document.TimeStamp descending
                                  select document;

            var eariler_documents = from document in collection.AsQueryable<Database.Registers>()
                                    where document.ServoID == servoID
                                    where document.TimeStamp >= minTime
                                    where document.TimeStamp <= this.time
                                    orderby document.TimeStamp ascending
                                    select document;
            
            foreach (var document in later_documents)
            {
                lposition = document.RegisterValues[RegisterType.PresentPosition.ToString()];
                ltime = document.TimeStamp;
            }
            
            //result.Add(position, time);
            foreach (var document in eariler_documents)
            {
                eposition = document.RegisterValues[RegisterType.PresentPosition.ToString()];
                etime = document.TimeStamp;
            }
            
            if (later_documents.Count()==0 && eariler_documents.Count() == 0)
            {
                var result = "nodate";
                return new { result };
            }else if (later_documents.Count()== 0)
            {
                var result = new Dictionary<string, long>();
                result.Add("ClosestSample", eposition);
                return new { result };
            }
            else if (eariler_documents.Count() == 0)
            {
                var result = new Dictionary<string, long>();
                result.Add("ClosestSample", lposition);
                return new { result };
            }
            else
            {
                var result = new Dictionary<string, double>();
                var position = interpolatedPosition(etime, eposition, ltime, lposition);
                result.Add("ServoID", servoID);
                result.Add("Samples in range", later_documents.Count() + eariler_documents.Count());
                result.Add("closetSampleTimeDifference- (s)", TimeSpan.FromTicks(etime.Ticks).TotalSeconds - TimeSpan.FromTicks(this.time.Ticks).TotalSeconds);
                result.Add("-position", eposition);
                result.Add("closetSampleTimeDifference+ (s)", TimeSpan.FromTicks(ltime.Ticks).TotalSeconds - TimeSpan.FromTicks(this.time.Ticks).TotalSeconds);
                result.Add("+position", lposition);
                result.Add("result", position);
                return new { result };
            }
            
        }
        

        private double interpolatedPosition(DateTime timeFrom, int positionFrom, DateTime timeTo, int positionTo)
        {
            var timeFromTo = timeTo - timeFrom;
            var timeTarget = this.time - timeFrom ;
            if (timeFromTo.Ticks == 0)
            {
                return (positionFrom + positionTo) * 0.5;
            }
            else
            {
                var resultPosition = positionFrom + (positionTo - positionFrom) * timeTarget.Ticks / timeFromTo.Ticks;
                return resultPosition;
            }
        }
    }


}


