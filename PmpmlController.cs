using pmpml.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Net;
using System.Web.Mvc;
using transit_realtime;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Reflection;

namespace pmpml.Controllers
{
    public class PmpmlController : Controller
    {
        private PmpmlDbContext dbContext = new PmpmlDbContext();
        #region realtime api
        /// <summary>
        /// This api returns the Trip updates
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        public JsonResult GetTripUpdates()
        {
            try
            {
                string userName = ConfigurationManager.AppSettings["userName"];
                string password = ConfigurationManager.AppSettings["password"];
                string url = "http://117.232.125.138/tms/data/gtfs/trip-updates.pb";
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(string.Format(url));
                webReq.Method = "GET";

                NetworkCredential networkCredential = new NetworkCredential(userName, password);
                CredentialCache myCredentialCache = new CredentialCache { { new Uri(url), "Basic", networkCredential } };
                webReq.PreAuthenticate = true;
                webReq.Credentials = myCredentialCache;

                HttpWebResponse webResponse = (HttpWebResponse)webReq.GetResponse();

                //I don't use the response for anything right now. But I might log the response answer later on.   
                Stream answer = webResponse.GetResponseStream();
                StreamReader _recivedAnswer = new StreamReader(answer);
                FeedMessage message = ProtoBuf.Serializer.Deserialize<FeedMessage>(_recivedAnswer.BaseStream);

                return Json(new { Result = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Result = "Error", Message = "Something went wrong." + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method returns the vehicle positions
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        public JsonResult VehiclePosition()
        {
            try
            {
                string userName = ConfigurationManager.AppSettings["userName"];
                string password = ConfigurationManager.AppSettings["password"];
                string url = "http://117.232.125.138/tms/data/gtfs/vehicle-positions.pb";
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(string.Format(url));
                webReq.Method = "GET";

                NetworkCredential networkCredential = new NetworkCredential(userName, password);
                CredentialCache myCredentialCache = new CredentialCache { { new Uri(url), "Basic", networkCredential } };
                webReq.PreAuthenticate = true;
                webReq.Credentials = myCredentialCache;

                HttpWebResponse webResponse = (HttpWebResponse)webReq.GetResponse();

                //I don't use the response for anything right now. But I might log the response answer later on.   
                Stream answer = webResponse.GetResponseStream();
                StreamReader _recivedAnswer = new StreamReader(answer);

                FeedMessage message = ProtoBuf.Serializer.Deserialize<FeedMessage>(_recivedAnswer.BaseStream);

                //var resct = _recivedAnswer.ReadToEnd();
                return Json(new { Result = message }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(new { Result = "Error", Message = "Something went wrong." + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion




        #region OneWay

        /// <summary>
        /// This method is for autocomplete field for one way search
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public JsonResult GetDropDownValuesForRouteAndStop(string searchString)
        {
            try
            {
                List<Routes> routes = (from c in dbContext.Routes.Where(t => t.route_id.StartsWith(searchString)) select c).Take(10).ToList(); //Get values from routes table according to search param
                List<Stops> stops = (from s in dbContext.Stops.Where(t => t.stop_name.StartsWith(searchString)) select s).Take(20).ToList();
                if (stops != null)
                {
                    stops = stops.GroupBy(p => p.stop_name).Select(t => t.First()).ToList();
                }
                return Json(new { Routs = routes, Stops = stops }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }



        /// <summary>
        /// This method is for getting vehicle by route id from transit-realtime api
        /// </summary>
        /// <param name="routeid"></param>
        /// <returns></returns>
        public JsonResult GetVehiclePositionFromRouteId(string routeid)
        {
            try
            {
                var resultPosition = VehiclePosition().Data;
                var jsonPosition = JsonConvert.SerializeObject(resultPosition.GetType().GetProperty("Result").GetValue(resultPosition, null));
                FeedMessage vehiclePosition = JsonConvert.DeserializeObject<FeedMessage>(jsonPosition);

                var resultTrip = GetTripUpdates().Data;
                var jsonTrip = JsonConvert.SerializeObject(resultTrip.GetType().GetProperty("Result").GetValue(resultTrip, null));
                FeedMessage tripUpdate = JsonConvert.DeserializeObject<FeedMessage>(jsonTrip);

                List<FeedEntity> tripData = tripUpdate.entity.Where(t => string.Equals(GetRoutIdFromTripId(t.trip_update.trip.trip_id), routeid)).ToList();
                //tripData = tripData.OrderBy(t => (t.trip_update.stop_time_update.FirstOrDefault()) != null ? Convert.ToDateTime(FormatTimeStamp((t.trip_update.stop_time_update.FirstOrDefault()).departure.time)) : Convert.ToDateTime("23:59:59")).ToList();

                List<FeedEntity> vehiclePositionData = (from vp in vehiclePosition.entity
                                                        from td in tripData
                                                        where vp.vehicle.trip != null && vp.vehicle.trip.trip_id == td.trip_update.trip.trip_id
                                                        select vp).ToList();

                return Json(new { TripList = FormatTripData(tripData, vehiclePositionData) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Get the route id from trip id by parsing the trip_id
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string GetRoutIdFromTripId(string param)
        {
            string[] words;
            string routId;
            if (param.Contains('|'))
            {
                words = param.Split('|');
                routId = words[1];
            }
            else
            {
                words = param.Split('_');
                routId = words[1];
            }
            return routId;
        }



        /// <summary>
        /// This method is for getting vehicle by stop id from transit-realtime api
        /// </summary>
        /// <param name="stopId"></param>
        /// <returns></returns>
        public JsonResult GetVehiclePositionFromStopId(int stopId)
        {
            try
            {
                var resultPosition = VehiclePosition().Data;
                //var jsonPosition = JsonConvert.SerializeObject(((resultPosition.GetType().GetProperty("Result")) != null ? (resultPosition.GetType().GetProperty("Result")) : (resultPosition.GetType().GetProperty("result")).GetValue(resultPosition, null)));
                var jsonPosition = JsonConvert.SerializeObject(resultPosition.GetType().GetProperty("Result").GetValue(resultPosition, null));
                FeedMessage vehiclePosition = JsonConvert.DeserializeObject<FeedMessage>(jsonPosition);

                var resultTrip = GetTripUpdates().Data;
                var jsonTrip = JsonConvert.SerializeObject(resultTrip.GetType().GetProperty("Result").GetValue(resultTrip, null));
                FeedMessage tripUpdate = JsonConvert.DeserializeObject<FeedMessage>(jsonTrip);
                //FeedMessage vehiclePosition = GetDeserializedVehiclePositionData();
                //FeedMessage tripUpdate = GetDeserializedTripUpdatesData();
                if (vehiclePosition.entity.Count == 0 || tripUpdate.entity.Count == 0)
                    return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);

                List<FeedEntity> tripData = (from f in tripUpdate.entity
                                             from stu in f.trip_update.stop_time_update
                                             where stu.stop_id.Equals(stopId.ToString())
                                             select f).ToList();

                List<FeedEntity> vehiclePositionData = (from vp in vehiclePosition.entity
                                                        from td in tripData
                                                        where vp.vehicle.trip != null && vp.vehicle.trip.trip_id == td.trip_update.trip.trip_id
                                                        select vp).ToList();

                return Json(new { TripList = FormatTripData(tripData, vehiclePositionData) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to formate the realtime data to required format.
        /// </summary>
        /// <param name="tripData"></param>
        /// <param name="vehiclePositionData"></param>
        /// <returns></returns>
        private List<TripsFlattenedModel> FormatTripData(List<FeedEntity> tripData, List<FeedEntity> vehiclePositionData)
        {
            List<TripsFlattenedModel> trips = new List<TripsFlattenedModel>();
            List<Stops> stopsData = dbContext.Stops.ToList();
            foreach (FeedEntity trip in tripData)
            {
                VehicleFlattenedModel vehicle = (from vpd in vehiclePositionData.Where(v => v.vehicle.trip.trip_id == trip.trip_update.trip.trip_id)
                                                 select new VehicleFlattenedModel
                                                 {
                                                     BusLatitude = vpd.vehicle.position.latitude,
                                                     BusLongitude = vpd.vehicle.position.longitude,
                                                     BusNumber = vpd.vehicle.vehicle.id,
                                                     CurrentStopSequence = (int)vpd.vehicle.current_stop_sequence
                                                 }).FirstOrDefault();

                List<StopsFlattenedModel> stops = (from t in trip.trip_update.stop_time_update
                                                   from s in stopsData
                                                   where t.stop_id.Equals(s.stop_code.ToString())
                                                   select new StopsFlattenedModel
                                                   {
                                                       StopArrivalTime = t.arrival != null ? FormatTimeStamp(t.arrival.time) : "",
                                                       ArrivalDelay = t.arrival.delay,
                                                       StopDepartureTime = t.departure != null ? FormatTimeStamp(t.departure.time) : "",
                                                       DepartureDelay = t.departure.delay,
                                                       StopId = Convert.ToInt32(t.stop_id),
                                                       StopSequence = (int)t.stop_sequence,
                                                       StopName = s.stop_name,
                                                       StopLatitude = s.stop_lat,
                                                       StopLongitude = s.stop_lon
                                                   }).OrderBy(t => t.StopSequence).ToList();

                if (stops.Count() != 0 && vehicle != null)
                {
                    TripsFlattenedModel tripModel = new TripsFlattenedModel
                    {
                        RouteId = GetRoutIdFromTripId(trip.trip_update.trip.trip_id),
                        TripId = trip.trip_update.trip.trip_id,
                        Stops = stops,
                        VehiclePosition = vehicle,
                        SourceDepartureTime = stops[0].StopDepartureTime
                    };

                    trips.Add(tripModel);
                }
            }
            return trips.OrderBy(t => Convert.ToDateTime(t.SourceDepartureTime)).ToList();
        }

        private string FormatTimeStamp(long timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var time = epoch.AddSeconds(timestamp).ToLocalTime().ToString("HH:mm:ss");
            return time;
        }

        /// <summary>
        /// This method is to get trips for the roots searched by date
        /// </summary>
        /// <param name="rootId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public JsonResult GetTripsForRoot(string rootId, DateTime date)
        {
            try
            {
                string day = date.ToString("dddd");
                List<Calendar> cal = dbContext.Calendar.ToList();
                int[] service_ids = cal.Where(c => (int)(c.GetType().GetRuntimeProperty(day.ToLower()).GetValue(c)) == 1).Select(c => c.service_id).ToArray();

                if (service_ids.Count() == 0)
                    return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);

                List<Trips> trips = (from t in dbContext.Trips
                                     from c in dbContext.Calendar
                                     where t.route_id.Equals(rootId)
                                     && t.service_id.Equals(c.service_id)
                                     && service_ids.Contains(c.service_id)
                                     select t).ToList();
                //dbContext.Trips.Where(t => t.route_id == rootId).ToList();

                List<TripsFlattenedModel> tripdata = new List<TripsFlattenedModel>();
                foreach (Trips tr in trips)
                {
                    List<Stop_times> stoptimes = dbContext.StopTimes.Where(t => t.trip_id == tr.trip_id).OrderBy(t => t.stop_sequence).ToList();
                    Stop_times sourceStopTimesObj = stoptimes.First();
                    Stop_times destinationStopTimesObj = stoptimes.Last();
                    tripdata.Add(new TripsFlattenedModel { RouteId = tr.route_id, TripId = tr.trip_id, ShapeId = tr.shape_id, SourceSequence = sourceStopTimesObj.stop_sequence, DestinationSequence = destinationStopTimesObj.stop_sequence, SourceArrivalTime = sourceStopTimesObj.arrival_time, SourceDepartureTime = sourceStopTimesObj.departure_time, DestinationArrivalTime = destinationStopTimesObj.arrival_time, DestinationDepartureTime = destinationStopTimesObj.departure_time });
                }

                return Json(new { TripList = tripdata.OrderBy(t => Convert.ToDateTime(t.SourceDepartureTime)) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to get trips according to stops searched by date
        /// </summary>
        /// <param name="stopId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public JsonResult GetTripsByStop(int stopId, DateTime date)
        {
            try
            {
                string day = date.ToString("dddd");
                List<Calendar> cal = dbContext.Calendar.ToList();
                int[] service_ids = cal.Where(c => (int)(c.GetType().GetRuntimeProperty(day.ToLower()).GetValue(c)) == 1).Select(c => c.service_id).ToArray();

                if (service_ids.Count() == 0)
                    return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);

                List<TripsFlattenedModel> tripdata = new List<TripsFlattenedModel>();
                List<Stop_routes> routedata = (from c in dbContext.StopRoutes.Where(t => t.stop_id == stopId) select c).ToList();
                routedata.ForEach(tr =>
                {
                    if (tr != null)
                    {
                        var source = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(tr.source)) select s).FirstOrDefault();
                        var destination = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(tr.destination)) select s).FirstOrDefault();

                        int sourceId = source != null ? source.stop_id : 0;
                        int destinationId = destination != null ? destination.stop_id : 0;

                        if (sourceId != 0 && destinationId != 0)
                        {
                            List<Stop_times> sourceStopTimes = dbContext.StopTimes.Where(t => t.stop_id == sourceId).ToList();
                            List<Stop_times> destinationStopTimes = dbContext.StopTimes.Where(t => t.stop_id == destinationId).ToList();
                            List<Trips> tripdatatemp = (from t in dbContext.Trips
                                                        from c in dbContext.Calendar
                                                        where t.route_id.Equals(tr.route_id)
                                                        && t.service_id.Equals(c.service_id)
                                                        && service_ids.Contains(c.service_id)
                                                        select t).ToList();
                            //(from c in dbContext.Trips.Where(t => t.route_id == tr.route_id) select c).ToList();

                            if (sourceStopTimes != null && destinationStopTimes != null && tripdatatemp != null)
                            {
                                tripdatatemp.ForEach(trip =>
                                {
                                    Stop_times sourceStopTimesObj = sourceStopTimes.FirstOrDefault(t => t.trip_id == trip.trip_id);
                                    Stop_times destinationStopTimesObj = destinationStopTimes.FirstOrDefault(t => t.trip_id == trip.trip_id);
                                    if (sourceStopTimesObj != null && destinationStopTimesObj != null)
                                    {
                                        tripdata.Add(new TripsFlattenedModel { RouteId = trip.route_id, TripId = trip.trip_id, ShapeId = trip.shape_id, SourceSequence = sourceStopTimesObj.stop_sequence, DestinationSequence = destinationStopTimesObj.stop_sequence, SourceArrivalTime = sourceStopTimesObj.arrival_time, SourceDepartureTime = sourceStopTimesObj.departure_time, DestinationArrivalTime = destinationStopTimesObj.arrival_time, DestinationDepartureTime = destinationStopTimesObj.departure_time });
                                    }
                                });
                            }
                        }
                    }
                });
                return Json(new { TripList = tripdata.OrderBy(t => Convert.ToDateTime(t.SourceDepartureTime)) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to get value from Dashboard table 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public JsonResult GetDashboardValue()
        {
            List<Dashboard> dashboard = (from c in dbContext.Dashboard select c).ToList(); //Get values from Dashboard table
            return Json(new { Dashboard = dashboard }, JsonRequestBehavior.AllowGet);
        }

        #endregion
        [HttpPost]
        public JsonResult UpdateDashboardInfo(Dashboard DashboardInfo)
        {
            bool updateStatus = false;
            if(DashboardInfo != null)
            {
                dbContext.Entry(DashboardInfo).State = System.Data.Entity.EntityState.Modified;
                dbContext.SaveChanges();
                updateStatus = true;
            }
        
            return Json(new { Result = updateStatus }, JsonRequestBehavior.AllowGet);
        }

        #region RoundTrip
        /// <summary>
        /// This method is to get the list of routes and stops for source autocomplete
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public JsonResult GetDropdownValuesforRoundTripSource(string searchString)
        {
            try
            {
                List<Stops> stops = (from s in dbContext.Stops.Where(t => t.stop_name.StartsWith(searchString)) select s).Take(20).ToList();
                if (stops != null)
                {
                    stops = stops.GroupBy(p => p.stop_name).Select(t => t.First()).ToList();
                }

                //List<Stops> stops = (from s in dbContext.Stops.Where(t => t.stop_name.StartsWith(searchString)) select s).GroupBy(s => s.stop_name).Select(g => g.First()).Take(10).ToList();
                List<Stop_routes> routes = (from r in dbContext.StopRoutes.Where(t => t.source.StartsWith(searchString)).GroupBy(p => p.route_id) select r.FirstOrDefault()).Take(10).ToList();
                return Json(new { stops = stops, routes = routes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { stops = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to get the list of stops for destination autocomplete
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public JsonResult GetDropdownValuesForRoundTripDestination(string searchString)
        {
            try
            {
                List<Stops> stops = (from s in dbContext.Stops.Where(t => t.stop_name.StartsWith(searchString)) select s).Take(20).ToList();
                if (stops != null)
                {
                    stops = stops.GroupBy(p => p.stop_name).Select(t => t.First()).ToList();
                }


                return Json(new { stops = stops }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { stops = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// this method is to get the buses and stop lists from transit-realtime api
        /// </summary>
        /// <param name="sourceStop"></param>
        /// <param name="destinationStop"></param>
        /// <returns></returns>
        public JsonResult GetBussesFromOneStopToAnother(string sourceStop, string destinationStop)
        {
            try
            {
                var tripUpdateFeed = GetTripUpdates().Data;
                var tripJson = JsonConvert.SerializeObject(tripUpdateFeed.GetType().GetProperty("Result").GetValue(tripUpdateFeed, null));
                FeedMessage tripUpdate = JsonConvert.DeserializeObject<FeedMessage>(tripJson);

                var positionFeed = VehiclePosition().Data;
                var positionJson = JsonConvert.SerializeObject(positionFeed.GetType().GetProperty("Result").GetValue(positionFeed, null));
                FeedMessage vehiclePosition = JsonConvert.DeserializeObject<FeedMessage>(positionJson);

                int sourceId = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(sourceStop)) select s).FirstOrDefault().stop_id;
                int destinationId = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(destinationStop)) select s).FirstOrDefault().stop_id;

                List<FeedEntity> tripData = (from f in tripUpdate.entity
                                             from stu in f.trip_update.stop_time_update
                                             from stu2 in f.trip_update.stop_time_update
                                             where stu.stop_id.Equals(sourceId.ToString())
                                             && stu2.stop_id.Equals(destinationId.ToString())
                                             && stu.stop_sequence < stu2.stop_sequence
                                             select f).ToList();

                List<FeedEntity> vehiclePositionData = (from vp in vehiclePosition.entity
                                                        from td in tripData
                                                        where vp.vehicle.trip != null && vp.vehicle.trip.trip_id == td.trip_update.trip.trip_id
                                                        select vp).ToList();
                return Json(new { TripList = FormatTripData(tripData, vehiclePositionData) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to get the list of stops between one stop to another. searched by date
        /// </summary>
        /// <param name="sourceStop"></param>
        /// <param name="destinationStop"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public JsonResult GetListOfTripsFromOneStopToAnotherByDate(string sourceStop, string destinationStop, DateTime date)
        {
            try
            {
                string day = date.ToString("dddd");
                List<Calendar> cal = dbContext.Calendar.ToList();
                int[] service_ids = cal.Where(c => (int)(c.GetType().GetRuntimeProperty(day.ToLower()).GetValue(c)) == 1).Select(c => c.service_id).ToArray();

                if (service_ids.Count() == 0)
                    return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);

                List<Stops> sources = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(sourceStop)) select s).ToList();
                List<Stops> destinations = (from s in dbContext.Stops.Where(t => t.stop_name.Equals(destinationStop)) select s).ToList();

                List<TripsFlattenedModel> TripsList = new List<TripsFlattenedModel>();
                foreach (Stops source in sources)
                {
                    foreach (Stops destination in destinations)
                    {
                        int sourceId = source.stop_id;
                        int destinationId = destination.stop_id;
                        List<Stop_times> sourceStopTimes = (from st in dbContext.StopTimes.Where(t => t.stop_id == sourceId) select st).ToList();
                        List<Stop_times> destinationStopTimes = (from st in dbContext.StopTimes.Where(t => t.stop_id == destinationId) select st).ToList();
                        List<string> commonTripIds = (from st1 in sourceStopTimes
                                                      from st2 in destinationStopTimes
                                                      where st1.trip_id.Equals(st2.trip_id)
                                                      select st1.trip_id).Distinct().ToList();

                        foreach (string tripId in commonTripIds)
                        {
                            int sourceSequenceNo = (from st in sourceStopTimes.Where(t => t.trip_id.Equals(tripId)) select st).FirstOrDefault().stop_sequence;
                            int destinationSequenceNo = (from st in destinationStopTimes.Where(t => t.trip_id.Equals(tripId)) select st).FirstOrDefault().stop_sequence;
                            //if (sourceSequenceNo >= destinationSequenceNo)
                            //{
                            //    continue;
                            //}
                            //Trips trip = dbContext.Trips.Where(t => t.trip_id.Equals(tripId)).FirstOrDefault();
                            Trips trip = (from t in dbContext.Trips
                                          from c in dbContext.Calendar
                                          where t.trip_id.Equals(tripId)
                                          && t.service_id.Equals(c.service_id)
                                          && service_ids.Contains(c.service_id) 
                                          select t).FirstOrDefault();
                            if (trip != null)
                            {
                                Stop_times sourceStopTimesObj = sourceStopTimes.Where(t => t.stop_sequence == sourceSequenceNo && t.trip_id == tripId).FirstOrDefault();
                                Stop_times destinationStopTimesObj = destinationStopTimes.Where(t => t.stop_sequence == destinationSequenceNo && t.trip_id == tripId).FirstOrDefault();
                                TripsList.Add(new TripsFlattenedModel { RouteId = trip.route_id, TripId = tripId, ShapeId = trip.shape_id, SourceSequence = sourceSequenceNo, DestinationSequence = destinationSequenceNo, SourceArrivalTime = sourceStopTimesObj.arrival_time, SourceDepartureTime = sourceStopTimesObj.departure_time, DestinationArrivalTime = destinationStopTimesObj.arrival_time, DestinationDepartureTime = destinationStopTimesObj.departure_time });
                            }
                        }
                    }
                }
                return Json(new { TripList = TripsList.OrderBy(t => Convert.ToDateTime(t.SourceDepartureTime)), shapes = "success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// This method is to the the detail of selected trip.
        /// </summary>
        /// <param name="tripsFlattened"></param>
        /// <returns></returns>
        public JsonResult GetStopsDetailByTripId(TripsFlattenedModel tripsFlattened)
        {
            try
            {
                Trips trip = dbContext.Trips.Where(t => t.trip_id.Equals(tripsFlattened.TripId)).FirstOrDefault();
                if (trip != null)
                {
                    List<Shapes> shapes = dbContext.Shapes.Where(s => s.shape_id == trip.shape_id).ToList();
                    List<Stop_times> stopTimes = dbContext.StopTimes.Where(st => st.trip_id.Equals(trip.trip_id)).ToList();
                    List<Stops> stops = dbContext.Stops.ToList();
                    if (shapes.Count != 0 && stopTimes.Count != 0 && stops.Count != 0)
                    {
                        List<StopsFlattenedModel> stopsList = (from s in stops
                                                               from sh in shapes
                                                               from st in stopTimes
                                                               where trip.trip_id.Equals(tripsFlattened.TripId)
                                                               && sh.shape_id == trip.shape_id
                                                               && trip.trip_id.Equals(st.trip_id)
                                                               && st.stop_sequence == sh.shape_pt_sequence
                                                               && st.stop_id == s.stop_id
                                                               && st.stop_sequence >= tripsFlattened.SourceSequence
                                                               && st.stop_sequence <= tripsFlattened.DestinationSequence
                                                               select new StopsFlattenedModel
                                                               {
                                                                   StopLatitude = sh.shape_pt_lat,
                                                                   StopLongitude = sh.shape_pt_lon,
                                                                   StopArrivalTime = st.arrival_time,
                                                                   StopDepartureTime = st.departure_time,
                                                                   StopName = s.stop_name,
                                                                   StopId = s.stop_id,
                                                                   StopSequence = sh.shape_pt_sequence
                                                               }).OrderBy(p => p.StopSequence).ToList();
                        tripsFlattened.Stops = stopsList;
                    }
                }


                return Json(new { TripsDetails = tripsFlattened }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region track my bus

        public JsonResult TrackMyBus(int stopId)
        {
            var resultPosition = VehiclePosition().Data;
            //var jsonPosition = JsonConvert.SerializeObject(((resultPosition.GetType().GetProperty("Result")) != null ? (resultPosition.GetType().GetProperty("Result")) : (resultPosition.GetType().GetProperty("result")).GetValue(resultPosition, null)));
            var jsonPosition = JsonConvert.SerializeObject(resultPosition.GetType().GetProperty("Result").GetValue(resultPosition, null));
            FeedMessage vehiclePosition = JsonConvert.DeserializeObject<FeedMessage>(jsonPosition);

            var resultTrip = GetTripUpdates().Data;
            var jsonTrip = JsonConvert.SerializeObject(resultTrip.GetType().GetProperty("Result").GetValue(resultTrip, null));
            FeedMessage tripUpdate = JsonConvert.DeserializeObject<FeedMessage>(jsonTrip);

            if (vehiclePosition.entity.Count == 0 || tripUpdate.entity.Count == 0)
                return Json(new { TripList = "Error" }, JsonRequestBehavior.AllowGet);

            List<FeedEntity> tripData = (from f in tripUpdate.entity
                                         from stu in f.trip_update.stop_time_update
                                         where stu.stop_id.Equals(stopId.ToString())
                                         select f).ToList();

            List<FeedEntity> vehiclePositionData = (from vp in vehiclePosition.entity
                                                    from td in tripData
                                                    where vp.vehicle.trip != null && vp.vehicle.trip.trip_id == td.trip_update.trip.trip_id
                                                    select vp).ToList();

            List<TripsFlattenedModel> trips = FormatTripData(tripData, vehiclePositionData);

            List<TripsFlattenedModel> tripsSelected = (from trip in trips
                                                       from t in trip.Stops
                                                       where t.StopId == stopId && trip.VehiclePosition.CurrentStopSequence <= t.StopSequence
                                                       select trip).ToList();

            return Json(new { TripList = tripsSelected }, JsonRequestBehavior.AllowGet);
        }

        #endregion


    }
}