﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;//

using Excel = Microsoft.Office.Interop.Excel;


//Refer to Fitbit Library

using Fitbit.Models;
using SleepMakeSense.Models;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;

//Refer to MathNet.Numerics Library for statistical analysis
using MathNet.Numerics.Statistics;


namespace SleepMakeSense.Controllers
{
    
    public class UserdatasController : Controller
    {
        
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Userdatas
        public ActionResult Index()
        {
            return View(db.Userdatas.ToList());
        }

        // GET: Userdatas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var userdata = db.Userdatas.Find(id);
            if (userdata == null)
            {
                return HttpNotFound();
            }
            return View(userdata);
        }

        public async Task<ActionResult> FactorQuestions()
        {
            if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Models.Database Db = new Models.Database();
                string userId = System.Web.HttpContext.Current.User.Identity.GetUserId();
                var dataQuery = from a in Db.UserQuestions
                                 where a.AspNetUserId.Equals(userId)
                                 select a;

                foreach (UserQuestion data in dataQuery)
                {
                    if(data.AspNetUserId == userId)
                    {
                        ViewBag.Message = "Enter Your Daily Habits";
                        MyViewModel viewModel = new MyViewModel();

                        viewModel.UserQuestion = data;
                        viewModel.Morning = false;
                        if (DateTime.UtcNow.AddHours(10).ToString("tt") == "AM") viewModel.Morning = true;

                        return View(viewModel);
                    }
                }

                return View("Callback");
            }
            else return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        // GET: Userdatas/Create
        public ActionResult Create()
        {
            return View();
        }

        public ActionResult FactorList()
        {
            return View("FactorList");
        }

        // POST: Userdatas/Create
        // To prevent excessive posting attack , please enable the specific property to be bound to.
        // For more information , http: Please refer to the //go.microsoft.com/fwlink/ LinkId = 317598?.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserdataId,DateStamp,MinutesAsleep,MinutesAwake,AwakeningsCount,TimeInBed,MinutesToFallAsleep,MinutesAfterWakeup,SleepEfficiency,CaloriesIn,Water,CaloriesOut,Steps,Distance,MinutesSedentary,MinutesLightlyActive,MinutesFairlyActive,MinutesVeryActive,ActivityCalories,Floors,TimeEnteredBed,Weight,BMI,Fat")] Userdata userdata)
        {
            var data = userdata;
            if (ModelState.IsValid)
            {
                db.Userdatas.Add(data);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userdata);
        }

        // GET: Userdatas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var userdata = db.Userdatas.Find(id);
            if (userdata == null)
            {
                return HttpNotFound();
            }
            return View(userdata);
        }

        // POST: Userdatas/Edit/5
        // To prevent excessive posting attack , please enable the specific property to be bound to 
        // For more information , http: Please refer to the //go.microsoft.com/fwlink/ LinkId = 317598?.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserdataId,DateStamp,MinutesAsleep,MinutesAwake,AwakeningsCount,TimeInBed,MinutesToFallAsleep,MinutesAfterWakeup,SleepEfficiency,CaloriesIn,Water,CaloriesOut,Steps,Distance,MinutesSedentary,MinutesLightlyActive,MinutesFairlyActive,MinutesVeryActive,ActivityCalories,Floors,TimeEnteredBed,Weight,BMI,Fat")] Userdata userdata)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userdata).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(userdata);
        }

        // GET: Userdatas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var userdata = db.Userdatas.Find(id);
            if (userdata == null)
            {
                return HttpNotFound();
            }
            return View(userdata);
        }

        // POST: Userdatas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var userdata = db.Userdatas.Find(id);
            db.Userdatas.Remove(userdata);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// HttpClient and hence FitbitClient are designed to be long-lived for the duration of the session. This method ensures only one client is created for the duration of the session.
        /// More info at: http://stackoverflow.com/questions/22560971/what-is-the-overhead-of-creating-a-new-httpclient-per-call-in-a-webapi-client
        /// </summary>
        /// <returns></returns>
        public FitbitClient GetFitbitClient(OAuth2AccessToken accessToken = null)
        {
            if (Session["FitbitClient"] == null)
            {
                if (accessToken != null)
                {
                    var appCredentials = (FitbitAppCredentials)Session["AppCredentials"];
                    FitbitClient client = new FitbitClient(appCredentials, accessToken);
                    Session["FitbitClient"] = client;
                    return client;
                }
                else
                {
                    throw new Exception("First time requesting a FitbitClient from the session you must pass the AccessToken.");
                }

            }
            else
            {
                return (FitbitClient)Session["FitbitClient"];
            }
        }

        /// <summary>
        /// Updates the Fitbit data in the Userdata on the database
        /// </summary>
        /// <param name="query"></param>
        /// <param name="item"></param>

        private void UpdateFitbitData (int queryId, bool queryFitbitData, Userdata item)
        {
            if(!queryFitbitData){
            Userdata updateQuery = (from a in db.Userdatas
                                    where a.Id == queryId
                                    select a).First();
            //Update database
            updateQuery.MinutesAsleep = item.MinutesAsleep;
            updateQuery.MinutesAwake = item.MinutesAwake;
            updateQuery.AwakeningsCount = item.AwakeningsCount;
            updateQuery.TimeInBed = item.TimeInBed;
            updateQuery.MinutesToFallAsleep = item.MinutesToFallAsleep;
            updateQuery.MinutesAfterWakeup = item.MinutesAfterWakeup;
            updateQuery.SleepEfficiency = item.SleepEfficiency;
            updateQuery.CaloriesIn = item.CaloriesIn;
            updateQuery.Water = item.Water;
            updateQuery.CaloriesOut = item.CaloriesOut;
            updateQuery.Steps = item.Steps;
            updateQuery.Distance = item.Distance;
            updateQuery.MinutesSedentary = item.MinutesSedentary;
            updateQuery.MinutesLightlyActive = item.MinutesLightlyActive;
            updateQuery.MinutesFairlyActive = item.MinutesFairlyActive;
            updateQuery.MinutesVeryActive = item.MinutesVeryActive;
            updateQuery.ActivityCalories = item.ActivityCalories;
            updateQuery.TimeEnteredBed = item.TimeEnteredBed;
            updateQuery.Weight = item.Weight;
            updateQuery.BMI = item.BMI;
            updateQuery.Fat = item.Fat;
            updateQuery.FitbitData = true;
            db.SaveChangesAsync();
            }
        }
          
        private ActionResult DiaryDataSync()
        {
            Userdata data = new Userdata();
            data.DateStamp = DateTime.UtcNow;
            int temp = 0;
 
            //TV - Need to add in Database and Model
            //ddlViewByWatchTV
            if (Convert.ToInt32(Request["ddlViewByWatchTV"].ToString()) != 0)
            {
                data.WatchTV = Convert.ToInt32(Request["ddlViewByWatchTV"].ToString());
            }
            //Caffiene
            if (Convert.ToInt32(Request["ddlViewByCoffee"].ToString()) != 0)
            {
                data.Coffee = Request["ddlViewByCoffee"].ToString();
            }
            //Caffiene2 - will need a switch statement
            // data.CoffeeTime = Request[""].ToString();
            int coffeeTime = Convert.ToInt32(Request["ddlViewByCoffeeTime"].ToString());
            //Exersise time - need to add in database and model
            //ddlViewByExerciseDuration
            if (Convert.ToInt32(Request["ddlViewByExerciseDuration"].ToString()) != 0)
            {
                data.ExerciseDuration = Convert.ToInt32(Request["ddlViewByExerciseDuration"].ToString());
            }
            //Exersise time 2 - need to add in database and model
            //ddlViewByExerciseIntensity
            if (Convert.ToInt32(Request["ddlViewByExerciseIntensity"].ToString()) != 0)
            {
                data.ExerciseIntensity = Convert.ToInt32(Request["ddlViewByExerciseIntensity"].ToString());
            }
            //Exersise time 3 -  need to add in database and model
            //ddlViewByExerciseType
            if (Convert.ToInt32(Request["ddlViewByExerciseType"].ToString()) != 0)
            {
                data.ExerciseType = Convert.ToInt32(Request["ddlViewByExerciseType"].ToString());
            }
            //Snack - need to add in database and model
            //ddlViewBySnack
            int snackTime = Convert.ToInt32(Request["ddlViewBySnack"].ToString());
            //Snack - need to add in database and model
            //ddlViewBySnack2
            temp = Convert.ToInt32(Request["ddlViewBySnack2"].ToString());
            if (temp != 0)
            {
                if (temp == 1) data.Snack2 = false;
                else data.Snack2 = true;
            }
            //Nap durateion - will need a switch statement
            if (Convert.ToInt32(Request["ddlViewByExerciseType"].ToString()) != 0)
            {
                data.NapDuration = Request["ddlViewByExerciseType"].ToString();
            }
            //NapTime - will need a switch statement
            int napTime = Convert.ToInt32(Request["ddlViewByNap2"].ToString());
            //Alcohol2
            if (Convert.ToInt32(Request["ddlViewByAlcohol"].ToString()) != 0)
            {
                data.Alcohol = Request["ddlViewByAlcohol"].ToString();
            }
            //Job 
            //ddlViewByJob - will require a switch statement
            int job = Convert.ToInt32(Request["ddlViewByJob"].ToString());
            //Job 2
            //ddlViewByJob2 
            if (Convert.ToInt32(Request["ddlViewByJob2"].ToString()) != 0)
            {
                data.Job2 = Convert.ToInt32(Request["ddlViewByJob2"].ToString());
            }
            //Time on phone
            //ddlViewByPhone
            if (Convert.ToInt32(Request["ddlViewByPhone"].ToString()) != 0)
            {
                data.Phone = Convert.ToInt32(Request["ddlViewByPhone"].ToString());
            }
            //Sleep Diary
            //ddlViewByDiary
            temp = Convert.ToInt32(Request["ddlViewByDiary"].ToString());
            if (temp != 0)
            {
                if (temp == 1) data.SleepDiary = false;
                else data.SleepDiary = true;
            }
            //Music
            //ddlViewByMusicDuration
            if (Convert.ToInt32(Request["ddlViewByMusicDuration"].ToString()) != 0)
            {
                data.Music = true;
                data.MusicDuration = Convert.ToInt32(Request["ddlViewByMusicDuration"].ToString());
            }

            //Type of Music 
            //ddlViewByMusicType
            if (Convert.ToInt32(Request["ddlViewByMusicType"].ToString()) != 0)
            {
                data.Music = true;
                data.MusicDuration = Convert.ToInt32(Request["ddlViewByMusicType"].ToString());
            }

            //Social Media
            //ddlViewBySocialMedia
            //Social
            if (Convert.ToInt32(Request["ddlViewBySocialMedia"].ToString()) != 0)
            {
                data.SocialActivity = Request["ddlViewBySocialMedia"].ToString();
            }

            //Games
            //ddlViewByGames
            if (Convert.ToInt32(Request["ddlViewByGames"].ToString()) != 0)
            {
                data.Games = Convert.ToInt32(Request["ddlViewByGames"].ToString());
            }
                //Exam/
                //ddlViewByAssessment
                if (Convert.ToInt32(Request["ddlViewByAssessment"].ToString()) == 0) data.Assessment = false;
                else data.Assessment = true;

            // Stressed

            if (Convert.ToInt32(Request["ddlViewByStress"].ToString()) != 0)
            {
                data.Stress = Request["ddlViewByStress"].ToString();
            }

            //switch statement for naptime
            switch (napTime)
            {
                case 1:
                    data.NapTime = "09.00";
                    break;
                case 2:
                    data.NapTime = "10.00";
                    break;
                case 3:
                    data.NapTime = "11.00";
                    break;
                case 4:
                    data.NapTime = "12.00";
                    break;
                case 5:
                    data.NapTime = "13.00";
                    break;
                case 6:
                    data.NapTime = "14.00";
                    break;
                case 7:
                    data.NapTime = "15.00";
                    break;
                case 8:
                    data.NapTime = "16.00";
                    break;
                case 9:
                    data.NapTime = "17.00";
                    break;
                case 10:
                    data.NapTime = "18.00";
                    break;
                case 11:
                    data.NapTime = "19.00";
                    break;
            }


            //switch statement for coffeeTime
            switch (coffeeTime)
            {
                case 1:
                    data.NapTime = "06.00";
                    break;
                case 2:
                    data.NapTime = "07.00";
                    break;
                case 3:
                    data.NapTime = "08.00";
                    break;
                case 4:
                    data.NapTime = "09.00";
                    break;
                case 5:
                    data.NapTime = "10.00";
                    break;
                case 6:
                    data.NapTime = "11.00";
                    break;
                case 7:
                    data.NapTime = "12.00";
                    break;
                case 8:
                    data.NapTime = "13.00";
                    break;
                case 9:
                    data.NapTime = "14.00";
                    break;
                case 10:
                    data.NapTime = "15.00";
                    break;
                case 11:
                    data.NapTime = "16.00";
                    break;
                case 12:
                    data.NapTime = "17.00";
                    break;
                case 13:
                    data.NapTime = "18.00";
                    break;
            }
            switch (snackTime)
            {
                case 1:
                    data.NapTime = "06.00";
                    break;
                case 2:
                    data.NapTime = "07.00";
                    break;
                case 3:
                    data.NapTime = "08.00";
                    break;
                case 4:
                    data.NapTime = "09.00";
                    break;
                case 5:
                    data.NapTime = "10.00";
                    break;
                case 6:
                    data.NapTime = "11.00";
                    break;
                case 7:
                    data.NapTime = "12.00";
                    break;
                case 8:
                    data.NapTime = "13.00";
                    break;
                case 9:
                    data.NapTime = "14.00";
                    break;
                case 10:
                    data.NapTime = "15.00";
                    break;
                case 11:
                    data.NapTime = "16.00";
                    break;
                case 12:
                    data.NapTime = "17.00";
                    break;
                case 13:
                    data.NapTime = "18.00";
                    break;
            }
            job
                napTime
                

            return View();



        }


        private async Task<ActionResult> FitbitDataSync(FitbitClient client, String userId)
        {
            ViewBag.FitbitSynced = true;


            bool userLogedIn = false;
            Models.Database Db = new Models.Database();
            List<Userdata> queryList = new List<Userdata>();

            DateTime dateStop = DateTime.UtcNow.Date.AddDays(-40);

            bool sync = true;

            userLogedIn = true; 
            var lastSynced = from a in Db.Userdatas
                             where a.AspNetUserId.Equals(userId) && a.FitbitData.Equals(false) && a.DateStamp >= dateStop
                             orderby a.DateStamp
                             select a;

            foreach (Userdata data in lastSynced)
            {
                if (data.DateStamp > dateStop)
                {
                    dateStop = data.DateStamp;
                }
                if (data.DateStamp.Date == DateTime.UtcNow.Date.AddDays(-1))
                {
                    sync = false;
                }
            }
            if (sync)
            {
                var dataQuery = from a in Db.Userdatas
                                where a.AspNetUserId.Equals(userId) && a.DateStamp >= dateStop
                                select a;
                foreach (Userdata data in dataQuery)
                {
                    queryList.Add(data);
                }

                List<Userdata> results = new List<Userdata>();

                var minutesAsleep = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesAsleep, dateStop, DateTime.UtcNow);
                var minutesAwake = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesAwake, dateStop, DateTime.UtcNow);
                var awakeningsCount = await client.GetTimeSeriesAsync(TimeSeriesResourceType.AwakeningsCount, dateStop, DateTime.UtcNow);
                var timeInBed = await client.GetTimeSeriesAsync(TimeSeriesResourceType.TimeInBed, dateStop, DateTime.UtcNow);
                var minutesToFallAsleep = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesToFallAsleep, dateStop, DateTime.UtcNow);
                var minutesAfterWakeup = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesAfterWakeup, dateStop, DateTime.UtcNow);
                var sleepEfficiency = await client.GetTimeSeriesAsync(TimeSeriesResourceType.SleepEfficiency, dateStop, DateTime.UtcNow);
                var caloriesIn = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesIn, dateStop, DateTime.UtcNow);
                var water = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Water, dateStop, DateTime.UtcNow);
                var caloriesOut = await client.GetTimeSeriesAsync(TimeSeriesResourceType.CaloriesOut, dateStop, DateTime.UtcNow);
                var steps = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Steps, dateStop, DateTime.UtcNow);
                var distance = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Distance, dateStop, DateTime.UtcNow);
                var minutesSedentary = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesSedentary, dateStop, DateTime.UtcNow);
                var minutesLightlyActive = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesLightlyActive, dateStop, DateTime.UtcNow);
                var minutesFairlyActive = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesFairlyActive, dateStop, DateTime.UtcNow);
                var minutesVeryActive = await client.GetTimeSeriesAsync(TimeSeriesResourceType.MinutesVeryActive, dateStop, DateTime.UtcNow);
                var activityCalories = await client.GetTimeSeriesAsync(TimeSeriesResourceType.ActivityCalories, dateStop, DateTime.UtcNow);
                var timeEnteredBed = await client.GetTimeSeriesAsync(TimeSeriesResourceType.TimeEnteredBed, dateStop, DateTime.UtcNow);
                var weight = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Weight, dateStop, DateTime.UtcNow);
                var bmi = await client.GetTimeSeriesAsync(TimeSeriesResourceType.BMI, dateStop, DateTime.UtcNow);
                var fat = await client.GetTimeSeriesAsync(TimeSeriesResourceType.Fat, dateStop, DateTime.UtcNow);

                foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesAsleep.DataList)
                {
                    if (Convert.ToDouble(data.Value) > 0)  // Remove entries with no sleep log (e.g. due to battery issue)
                    {
                        results.Add(new Userdata()
                        {
                            DateStamp = data.DateTime.Date.AddDays(-1),
                            MinutesAsleep = data.Value,
                            MinutesAwake = null,
                            AwakeningsCount = null,
                            TimeInBed = null,
                            MinutesToFallAsleep = null,
                            MinutesAfterWakeup = null,
                            SleepEfficiency = null,
                            WakeUpFreshness = null,
                            Coffee = null,
                            CoffeeTime = null,
                            Alcohol = null,
                            Mood = null,
                            Stress = null,
                            Tiredness = null,
                            Dream = null,
                            DigitalDev = null,
                            Light = null,
                            NapDuration = null,
                            NapTime = null,
                            SocialActivity = null,
                            DinnerTime = null,
                            AmbientTemp = null,
                            AmbientHumd = null,
                            ExerciseTime = null,
                            BodyTemp = null,
                            Hormone = null,
                            CaloriesIn = null,
                            Water = null,
                            CaloriesOut = null,
                            Steps = null,
                            Distance = null,
                            MinutesSedentary = null,
                            MinutesLightlyActive = null,
                            MinutesFairlyActive = null,
                            MinutesVeryActive = null,
                            ActivityCalories = null,
                            TimeEnteredBed = null,
                            Weight = null,
                            BMI = null,
                            Fat = null,
                            AspNetUserId = userId,
                            FitbitData = true
                        });
                    }
                }

                foreach (Userdata item in results)
                {
                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesAwake.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.MinutesAwake = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in awakeningsCount.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.AwakeningsCount = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in timeInBed.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.TimeInBed = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesToFallAsleep.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.MinutesToFallAsleep = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesAfterWakeup.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.MinutesAfterWakeup = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in sleepEfficiency.DataList.Where(data => data.DateTime.AddDays(-1) == item.DateStamp))
                    {
                        item.SleepEfficiency = data.Value;
                    }

                    // Potential impacting factors; need filter out untracked factors.
                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in caloriesIn.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.CaloriesIn = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in water.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.Water = data.Value;
                    }
                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in caloriesOut.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.CaloriesOut = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in steps.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.Steps = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in distance.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.Distance = data.Value;
                    }


                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesSedentary.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.MinutesSedentary = data.Value;
                        //if (Convert.ToDouble(item.MinutesSedentary) > 0) CNTminutesSedentary++; 
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesLightlyActive.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.MinutesLightlyActive = data.Value;
                        //if (Convert.ToDouble(item.MinutesLightlyActive) > 0) CNTminutesLightlyActive++; 
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesFairlyActive.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.MinutesFairlyActive = data.Value;
                        //if (Convert.ToDouble(item.MinutesFairlyActive) > 0) CNTminutesFairlyActive++; 
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in minutesVeryActive.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.MinutesVeryActive = data.Value;
                        //if (Convert.ToDouble(item.MinutesVeryActive) > 0) CNTminutesVeryActive++; 
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in activityCalories.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.ActivityCalories = data.Value;
                        //if (Convert.ToDouble(item.ActivityCalories) > 0) CNTactivityCalories++; 
                    }


                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in timeEnteredBed.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.TimeEnteredBed = data.Value;
                        //if (Convert.ToDouble(item.TimeEnteredBed) > 0) CNTtimeEnteredBed++; 
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in weight.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.Weight = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in bmi.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.BMI = data.Value;
                    }

                    foreach (Fitbit.Models.TimeSeriesDataList.Data data in fat.DataList.Where(data => data.DateTime == item.DateStamp))
                    {
                        item.Fat = data.Value;
                    }
                }
                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    //Console.Write(item); //didnt work!!!
                    // System.Diagnostics.Debug.Write(item.MinutesAsleep); // didnt work!!!!
                    //System.Diagnostics.Trace.Write(item); // didnt work!!!!

                    // ******** Add entry to DB !!! *************
                    if (userLogedIn)
                    {
                        bool entryAdded = false;
                        foreach (SleepMakeSense.Models.Userdata query in queryList)
                        {
                            if (query.DateStamp == item.DateStamp)
                            {
                                entryAdded = true;
                                UpdateFitbitData(query.Id, query.FitbitData, item);
                            }
                        }
                        if (entryAdded == false)
                        {
                            //add entry
                            db.Userdatas.Add(item);
                        }
                        db.SaveChanges();
                    }
                }
            }
            return View();
        }

        public async Task<ActionResult> Sync()
        {

            string userId = System.Web.HttpContext.Current.User.Identity.GetUserId(); ;
            FitbitClient client = GetFitbitClient();
       //     await FitbitDataSync(client, userId);
            MyViewModel model = DataModelCreation(userId);
            return View(model);
        }
/*
        private ActionResult Sync()
        {
            string userId = System.Web.HttpContext.Current.User.Identity.GetUserId(); ;
            MyViewModel model = DataModelCreation(userId);
            return View(model);
        }
*/
        public ActionResult NoDataSync()
        {
            return View("Sync");
        }
/*
        public async Task<ActionResult> DataSync()
        {
            string userId = System.Web.HttpContext.Current.User.Identity.GetUserId(); ;
            FitbitClient client = GetFitbitClient();
            await FitbitDataSync(client, userId);
            MyViewModel model = DataModelCreation(userId);
            return Sync();
        }
        */
        public MyViewModel DataModelCreation(string userId)
        {
            ViewBag.FitbitSynced = true;
            //Need to define it here. Cannot use the one defined in FitbitController.cs.

            Models.Database Db = new Models.Database();

            DateTime date = DateTime.UtcNow.AddDays(-40);

            List<Userdata> results = (from a in Db.Userdatas
                                      where a.AspNetUserId == userId && a.DateStamp >= date && a.FitbitData == true
                                     orderby  a.DateStamp
                                            select a).ToList();


            List<DiaryData> diaryData = new List<DiaryData>();
        

            int CNTcaloriesIn = 0, CNTwater = 0, CNTcaloriesOut = 0, CNTsteps = 0, CNTweight = 0, CNTfat = 0;
            int CNTwakeUpFreshness = 0, CNTcoffee = 0, CNTcoffeeTime = 0, CNTalcohol = 0, CNTmood = 0, CNTstress = 0,
                CNTtiredness = 0, CNTdream = 0, CNTdigitalDev = 0, CNTlight = 0, CNTnapDuration = 0, CNTnapTime = 0,
                CNTsocialActivity = 0, CNTdinnerTime = 0, CNTexerciseTime = 0, CNTambientTemp = 0, CNTambientHumd = 0,
                CNTbodyTemp = 0, CNThormone = 0;

            foreach (Userdata item in results)
            {


                    if (Convert.ToDouble(item.WakeUpFreshness) >= 0) CNTwakeUpFreshness++;
                    if (Convert.ToDouble(item.Coffee) >= 0) CNTcoffee++;
                    if (Convert.ToDouble(item.CoffeeTime) > 0) CNTcoffeeTime++;
                    if (Convert.ToDouble(item.Alcohol) >= 0) CNTalcohol++;
                    if (Convert.ToDouble(item.Mood) >= 0) CNTmood++;
                    if (Convert.ToDouble(item.Stress) >= 0) CNTstress++;
                    if (Convert.ToDouble(item.Tiredness) >= 0) CNTtiredness++;
                    if (Convert.ToDouble(item.Dream) >= 0) CNTdream++;
                    if (Convert.ToDouble(item.DigitalDev) >= 0) CNTdigitalDev++;
                    if (Convert.ToDouble(item.Light) >= 0) CNTlight++;
                    if (Convert.ToDouble(item.NapDuration) > 0) CNTnapDuration++;
                    if (Convert.ToDouble(item.NapTime) > 0) CNTnapTime++;
                    if (Convert.ToDouble(item.SocialActivity) >= 0) CNTsocialActivity++;
                    if (Convert.ToDouble(item.DinnerTime) > 0) CNTdinnerTime++;
                    if (Convert.ToDouble(item.ExerciseTime) > 0) CNTexerciseTime++;
                    if (Convert.ToDouble(item.AmbientTemp) > 0) CNTambientTemp++;
                    if (Convert.ToDouble(item.AmbientHumd) > 0) CNTambientHumd++;
                    if (Convert.ToDouble(item.BodyTemp) > 0) CNTbodyTemp++;
                    if (Convert.ToDouble(item.Hormone) > 0) CNThormone++;

                
            }


            int len = results.Count;

            List<CorrList> CoefficientList = new List<CorrList>();

            double[] MinutesAsleep = new double[len];
            double[] MinutesAwake = new double[len];
            double[] AwakeningsCount = new double[len];
            double[] MinutesToFallAsleep = new double[len];
            double[] SleepEfficiency = new double[len];

            double[] MinutesSedentary = new double[len];
            double[] MinutesLightlyActive = new double[len];
            double[] MinutesFairlyActive = new double[len];
            double[] MinutesVeryActive = new double[len];

            // should correlate to all tracked factors, including the ones tracked using diary       
            double[] WakeUpFreshnessCaloriesIn = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessWater = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessCaloriesOut = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessSteps = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessMinutesSedentary = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessMinutesLightlyActive = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessMinutesFairlyActive = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessMinutesVeryActive = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessWeight = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessFat = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessCoffee = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessCoffeeTime = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessAlcohol = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessMood = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessStress = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessTiredness = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessDream = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessDigitalDev = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessLight = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessNapDuration = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessNapTime = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessSocialActivity = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessDinnerTime = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessExerciseTime = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessAmbientTemp = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessAmbientHumd = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessBodyTemp = new double[CNTwakeUpFreshness];
            double[] WakeUpFreshnessHormone = new double[CNTwakeUpFreshness];

            double[] tmpCaloriesIn = new double[CNTwakeUpFreshness];
            double[] tmpWater = new double[CNTwakeUpFreshness];
            double[] tmpCaloriesOut = new double[CNTwakeUpFreshness];
            double[] tmpSteps = new double[CNTwakeUpFreshness];
            double[] tmpMinutesSedentary = new double[CNTwakeUpFreshness];
            double[] tmpMinutesLightlyActive = new double[CNTwakeUpFreshness];
            double[] tmpMinutesFairlyActive = new double[CNTwakeUpFreshness];
            double[] tmpMinutesVeryActive = new double[CNTwakeUpFreshness];
            double[] tmpWeight = new double[CNTwakeUpFreshness];
            double[] tmpFat = new double[CNTwakeUpFreshness];
            double[] tmpCoffee = new double[CNTwakeUpFreshness];
            double[] tmpCoffeeTime = new double[CNTwakeUpFreshness];
            double[] tmpAlcohol = new double[CNTwakeUpFreshness];
            double[] tmpMood = new double[CNTwakeUpFreshness];
            double[] tmpStress = new double[CNTwakeUpFreshness];
            double[] tmpTiredness = new double[CNTwakeUpFreshness];
            double[] tmpDream = new double[CNTwakeUpFreshness];
            double[] tmpDigitalDev = new double[CNTwakeUpFreshness];
            double[] tmpLight = new double[CNTwakeUpFreshness];
            double[] tmpNapDuration = new double[CNTwakeUpFreshness];
            double[] tmpNapTime = new double[CNTwakeUpFreshness];
            double[] tmpSocialActivity = new double[CNTwakeUpFreshness];
            double[] tmpDinnerTime = new double[CNTwakeUpFreshness];
            double[] tmpExerciseTime = new double[CNTwakeUpFreshness];
            double[] tmpAmbientTemp = new double[CNTwakeUpFreshness];
            double[] tmpAmbientHumd = new double[CNTwakeUpFreshness];
            double[] tmpBodyTemp = new double[CNTwakeUpFreshness];
            double[] tmpHormone = new double[CNTwakeUpFreshness];

            //double[] Floors = new double[len];
            //double[] TimeEnteredBed = new double[len];


            int iMinutesAsleep = 0;
            int iMinutesAwake = 0;
            int iAwakeningsCount = 0;
            int iMinutesToFallAsleep = 0;
            int iSleepEfficiency = 0;
            int iWakeUpFreshnessCaloriesIn = 0, iWakeUpFreshnessWater = 0, iWakeUpFreshnessCaloriesOut = 0,
                iWakeUpFreshnessSteps = 0, iWakeUpFreshnessMinutesSedentary = 0, iWakeUpFreshnessMinutesLightlyActive = 0,
                iWakeUpFreshnessMinutesFairlyActive = 0, iWakeUpFreshnessMinutesVeryActive = 0, iWakeUpFreshnessWeight = 0,
                iWakeUpFreshnessFat = 0, iWakeUpFreshnessCoffee = 0, iWakeUpFreshnessCoffeeTime = 0, iWakeUpFreshnessAlcohol = 0, iWakeUpFreshnessMood = 0,
                iWakeUpFreshnessStress = 0, iWakeUpFreshnessTiredness = 0, iWakeUpFreshnessDream = 0, iWakeUpFreshnessDigitalDev = 0, iWakeUpFreshnessLight = 0,
                iWakeUpFreshnessNapDuration = 0, iWakeUpFreshnessNapTime = 0, iWakeUpFreshnessSocialActivity = 0, iWakeUpFreshnessDinnerTime = 0,
                iWakeUpFreshnessExerciseTime = 0, iWakeUpFreshnessAmbientTemp = 0, iWakeUpFreshnessAmbientHumd = 0, iWakeUpFreshnessBodyTemp = 0, iWakeUpFreshnessHormone = 0;

            int iMinutesSedentary = 0;
            int iMinutesLightlyActive = 0;
            int iMinutesFairlyActive = 0;
            int iMinutesVeryActive = 0;

            //int iFloors = 0;
            //int iTimeEnteredBed = 0;



            foreach (SleepMakeSense.Models.Userdata item in results)
            {

                //Console.Write(item); //didnt work!!!
                // System.Diagnostics.Debug.Write(item.MinutesAsleep); // didnt work!!!!
                //System.Diagnostics.Trace.Write(item); // didnt work!!!!

                // ******** Add entry to DB !!! *************         

                MinutesAsleep[iMinutesAsleep++] = Convert.ToDouble(item.MinutesAsleep);
                MinutesAwake[iMinutesAwake++] = Convert.ToDouble(item.MinutesAwake);
                AwakeningsCount[iAwakeningsCount++] = Convert.ToDouble(item.AwakeningsCount);
                MinutesToFallAsleep[iMinutesToFallAsleep++] = Convert.ToDouble(item.MinutesToFallAsleep);
                SleepEfficiency[iSleepEfficiency++] = Convert.ToDouble(item.SleepEfficiency);

                MinutesSedentary[iMinutesSedentary++] = Convert.ToDouble(item.MinutesSedentary);
                MinutesLightlyActive[iMinutesLightlyActive++] = Convert.ToDouble(item.MinutesLightlyActive);
                MinutesFairlyActive[iMinutesFairlyActive++] = Convert.ToDouble(item.MinutesFairlyActive);
                MinutesVeryActive[iMinutesVeryActive++] = Convert.ToDouble(item.MinutesVeryActive);

                if (Convert.ToDouble(item.WakeUpFreshness) > 0)
                {

                    if (Convert.ToDouble(item.CaloriesIn) > 0)
                    {
                        WakeUpFreshnessCaloriesIn[iWakeUpFreshnessCaloriesIn] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpCaloriesIn[iWakeUpFreshnessCaloriesIn] = Convert.ToDouble(item.CaloriesIn);
                        iWakeUpFreshnessCaloriesIn++;
                    }
                    if (Convert.ToDouble(item.Water) > 0)
                    {
                        WakeUpFreshnessWater[iWakeUpFreshnessWater] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpWater[iWakeUpFreshnessWater] = Convert.ToDouble(item.Water);
                        iWakeUpFreshnessWater++;
                    }
                    if (Convert.ToDouble(item.CaloriesOut) > 0)
                    {
                        WakeUpFreshnessCaloriesOut[iWakeUpFreshnessCaloriesOut] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpCaloriesOut[iWakeUpFreshnessCaloriesOut] = Convert.ToDouble(item.CaloriesOut);
                        iWakeUpFreshnessCaloriesOut++;
                    }
                    if (Convert.ToDouble(item.Steps) > 0)
                    {
                        WakeUpFreshnessSteps[iWakeUpFreshnessSteps] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpSteps[iWakeUpFreshnessSteps] = Convert.ToDouble(item.Steps);
                        iWakeUpFreshnessSteps++;
                    }
                    if (Convert.ToDouble(item.MinutesSedentary) > 0)
                    {
                        WakeUpFreshnessMinutesSedentary[iWakeUpFreshnessMinutesSedentary] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpMinutesSedentary[iWakeUpFreshnessMinutesSedentary] = Convert.ToDouble(item.MinutesSedentary);
                        iWakeUpFreshnessMinutesSedentary++;
                    }
                    if (Convert.ToDouble(item.MinutesLightlyActive) >= 0)
                    {
                        WakeUpFreshnessMinutesLightlyActive[iWakeUpFreshnessMinutesLightlyActive] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpMinutesLightlyActive[iWakeUpFreshnessMinutesLightlyActive] = Convert.ToDouble(item.MinutesLightlyActive);
                        iWakeUpFreshnessMinutesLightlyActive++;
                    }
                    if (Convert.ToDouble(item.MinutesFairlyActive) >= 0)
                    {
                        WakeUpFreshnessMinutesFairlyActive[iWakeUpFreshnessMinutesFairlyActive] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpMinutesFairlyActive[iWakeUpFreshnessMinutesFairlyActive] = Convert.ToDouble(item.MinutesFairlyActive);
                        iWakeUpFreshnessMinutesFairlyActive++;
                    }
                    if (Convert.ToDouble(item.MinutesVeryActive) >= 0)
                    {
                        WakeUpFreshnessMinutesVeryActive[iWakeUpFreshnessMinutesVeryActive] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpMinutesVeryActive[iWakeUpFreshnessMinutesVeryActive] = Convert.ToDouble(item.MinutesVeryActive);
                        iWakeUpFreshnessMinutesVeryActive++;
                    }
                    if (Convert.ToDouble(item.Weight) > 0)
                    {
                        WakeUpFreshnessWeight[iWakeUpFreshnessWeight] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpWeight[iWakeUpFreshnessWeight] = Convert.ToDouble(item.Weight);
                        iWakeUpFreshnessWeight++;
                    }
                    if (Convert.ToDouble(item.Fat) > 0)
                    {
                        WakeUpFreshnessFat[iWakeUpFreshnessFat] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpFat[iWakeUpFreshnessFat] = Convert.ToDouble(item.Fat);
                        iWakeUpFreshnessFat++;
                    }
                    if (Convert.ToDouble(item.Coffee) >= 0)
                    {
                        WakeUpFreshnessCoffee[iWakeUpFreshnessCoffee] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpCoffee[iWakeUpFreshnessCoffee] = Convert.ToDouble(item.Coffee);
                        iWakeUpFreshnessCoffee++;
                    }
                    if (Convert.ToDouble(item.CoffeeTime) > 0)
                    {
                        WakeUpFreshnessCoffeeTime[iWakeUpFreshnessCoffeeTime] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpCoffeeTime[iWakeUpFreshnessCoffeeTime] = Convert.ToDouble(item.CoffeeTime);
                        iWakeUpFreshnessCoffeeTime++;
                    }
                    if (Convert.ToDouble(item.Alcohol) >= 0)
                    {
                        WakeUpFreshnessAlcohol[iWakeUpFreshnessAlcohol] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpAlcohol[iWakeUpFreshnessAlcohol] = Convert.ToDouble(item.Alcohol);
                        iWakeUpFreshnessAlcohol++;
                    }
                    if (Convert.ToDouble(item.Mood) >= 0)
                    {
                        WakeUpFreshnessMood[iWakeUpFreshnessMood] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpMood[iWakeUpFreshnessMood] = Convert.ToDouble(item.Mood);
                        iWakeUpFreshnessMood++;
                    }
                    if (Convert.ToDouble(item.Stress) >= 0)
                    {
                        WakeUpFreshnessStress[iWakeUpFreshnessStress] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpStress[iWakeUpFreshnessStress] = Convert.ToDouble(item.Stress);
                        iWakeUpFreshnessStress++;
                    }
                    if (Convert.ToDouble(item.Tiredness) >= 0)
                    {
                        WakeUpFreshnessTiredness[iWakeUpFreshnessTiredness] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpTiredness[iWakeUpFreshnessTiredness] = Convert.ToDouble(item.Tiredness);
                        iWakeUpFreshnessTiredness++;
                    }
                    if (Convert.ToDouble(item.Dream) >= 0)
                    {
                        WakeUpFreshnessDream[iWakeUpFreshnessDream] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpDream[iWakeUpFreshnessDream] = Convert.ToDouble(item.Dream);
                        iWakeUpFreshnessDream++;
                    }
                    if (Convert.ToDouble(item.DigitalDev) >= 0)
                    {
                        WakeUpFreshnessDigitalDev[iWakeUpFreshnessDigitalDev] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpDigitalDev[iWakeUpFreshnessDigitalDev] = Convert.ToDouble(item.DigitalDev);
                        iWakeUpFreshnessDigitalDev++;
                    }
                    if (Convert.ToDouble(item.Light) >= 0)
                    {
                        WakeUpFreshnessLight[iWakeUpFreshnessLight] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpLight[iWakeUpFreshnessLight] = Convert.ToDouble(item.Light);
                        iWakeUpFreshnessLight++;
                    }
                    if (Convert.ToDouble(item.NapDuration) >= 0)
                    {
                        WakeUpFreshnessNapDuration[iWakeUpFreshnessNapDuration] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpNapDuration[iWakeUpFreshnessNapDuration] = Convert.ToDouble(item.NapDuration);
                        iWakeUpFreshnessNapDuration++;
                    }
                    if (Convert.ToDouble(item.NapTime) > 0)
                    {
                        WakeUpFreshnessNapTime[iWakeUpFreshnessNapTime] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpNapTime[iWakeUpFreshnessNapTime] = Convert.ToDouble(item.NapTime);
                        iWakeUpFreshnessNapTime++;
                    }
                    if (Convert.ToDouble(item.SocialActivity) >= 0)
                    {
                        WakeUpFreshnessSocialActivity[iWakeUpFreshnessSocialActivity] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpSocialActivity[iWakeUpFreshnessSocialActivity] = Convert.ToDouble(item.SocialActivity);
                        iWakeUpFreshnessSocialActivity++;
                    }
                    if (Convert.ToDouble(item.DinnerTime) > 0)
                    {
                        WakeUpFreshnessDinnerTime[iWakeUpFreshnessDinnerTime] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpDinnerTime[iWakeUpFreshnessDinnerTime] = Convert.ToDouble(item.DinnerTime);
                        iWakeUpFreshnessDinnerTime++;
                    }
                    if (Convert.ToDouble(item.ExerciseTime) > 0)
                    {
                        WakeUpFreshnessExerciseTime[iWakeUpFreshnessExerciseTime] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpExerciseTime[iWakeUpFreshnessExerciseTime] = Convert.ToDouble(item.ExerciseTime);
                        iWakeUpFreshnessExerciseTime++;
                    }
                    if (Convert.ToDouble(item.AmbientTemp) > 0)
                    {
                        WakeUpFreshnessAmbientTemp[iWakeUpFreshnessAmbientTemp] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpAmbientTemp[iWakeUpFreshnessAmbientTemp] = Convert.ToDouble(item.AmbientTemp);
                        iWakeUpFreshnessAmbientTemp++;
                    }
                    if (Convert.ToDouble(item.AmbientHumd) > 0)
                    {
                        WakeUpFreshnessAmbientHumd[iWakeUpFreshnessAmbientHumd] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpAmbientHumd[iWakeUpFreshnessAmbientHumd] = Convert.ToDouble(item.AmbientHumd);
                        iWakeUpFreshnessAmbientHumd++;
                    }
                    if (Convert.ToDouble(item.BodyTemp) > 0)
                    {
                        WakeUpFreshnessBodyTemp[iWakeUpFreshnessBodyTemp] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpBodyTemp[iWakeUpFreshnessBodyTemp] = Convert.ToDouble(item.BodyTemp);
                        iWakeUpFreshnessBodyTemp++;
                    }
                    if (Convert.ToDouble(item.Hormone) > 0)
                    {
                        WakeUpFreshnessHormone[iWakeUpFreshnessHormone] = Convert.ToDouble(item.WakeUpFreshness);
                        tmpHormone[iWakeUpFreshnessHormone] = Convert.ToDouble(item.Hormone);
                        iWakeUpFreshnessHormone++;
                    }
                }

            }

            // WakeUpFreshness
            double rWakeUpFreshness = 0;

            if (iWakeUpFreshnessCaloriesIn > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessCaloriesIn, tmpCaloriesIn);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CaloriesIn", Coefficient = rWakeUpFreshness, Note = "The more calories you intook a day, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CaloriesIn", Coefficient = rWakeUpFreshness, Note = "The more calories you intook a day, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessWater > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessWater, tmpWater);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Water", Coefficient = rWakeUpFreshness, Note = "The more water you drunk a day, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Water", Coefficient = rWakeUpFreshness, Note = "The more water you drunk a day, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessCaloriesOut > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessCaloriesOut, tmpCaloriesOut);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CaloriesOut", Coefficient = rWakeUpFreshness, Note = "The more your calory output was, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CaloriesOut", Coefficient = rWakeUpFreshness, Note = "The more your calory output was, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessSteps > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessSteps, tmpSteps);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Steps", Coefficient = rWakeUpFreshness, Note = "The more steps you walked, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Steps", Coefficient = rWakeUpFreshness, Note = "The more steps you walked, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessMinutesSedentary > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessMinutesSedentary, tmpMinutesSedentary);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesSedentary", Coefficient = rWakeUpFreshness, Note = "The more sedentary you were, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesSedentary", Coefficient = rWakeUpFreshness, Note = "The more sedentary you were, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessMinutesLightlyActive > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessMinutesLightlyActive, tmpMinutesLightlyActive);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesLightlyActive", Coefficient = rWakeUpFreshness, Note = "The more light exercise you did, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesLightlyActive", Coefficient = rWakeUpFreshness, Note = "The more light exercise you did, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessMinutesFairlyActive > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessMinutesFairlyActive, tmpMinutesFairlyActive);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesFairlyActive", Coefficient = rWakeUpFreshness, Note = "The more moderate exercise you did, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesFairlyActive", Coefficient = rWakeUpFreshness, Note = "The more moderate exercise you did, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessMinutesVeryActive > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessMinutesVeryActive, tmpMinutesVeryActive);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesVeryActive", Coefficient = rWakeUpFreshness, Note = "The more intense exercise you did, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "MinutesVeryActive", Coefficient = rWakeUpFreshness, Note = "The more intense exercise you did, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessWeight > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessWeight, tmpWeight);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Weight", Coefficient = rWakeUpFreshness, Note = "The heavier your weight were, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Weight", Coefficient = rWakeUpFreshness, Note = "The heavier your weight were, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessFat > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessFat, tmpFat);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Fat", Coefficient = rWakeUpFreshness, Note = "The higher your fat rate were, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Fat", Coefficient = rWakeUpFreshness, Note = "The higher your fat rate were, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessCoffee > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessCoffee, tmpCoffee);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Coffee", Coefficient = rWakeUpFreshness, Note = "The more coffee you consumed, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Coffee", Coefficient = rWakeUpFreshness, Note = "The more coffee you consume, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessCoffeeTime > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessCoffeeTime, tmpCoffeeTime);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CoffeeTime", Coefficient = rWakeUpFreshness, Note = "The later you drunk coffee during the day, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "CoffeeTime", Coefficient = rWakeUpFreshness, Note = "The earlier you drunk coffee during the day, the more alert you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessAlcohol > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessAlcohol, tmpAlcohol);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Alcohol", Coefficient = rWakeUpFreshness, Note = "The more alcohol you drunk, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Alcohol", Coefficient = rWakeUpFreshness, Note = "The more alcohol you drunk, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessMood > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessMood, tmpMood);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Mood", Coefficient = rWakeUpFreshness, Note = "The happier you felt before bed time, the more alert you felt when you woke up." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Mood", Coefficient = rWakeUpFreshness, Note = "Oops! The happier you felt before bed time, the more dizzy you felt when you woke up." });
                    }
                }
            }
            if (iWakeUpFreshnessStress > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessStress, tmpStress);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Stress", Coefficient = rWakeUpFreshness, Note = "The more stressed you were before bed time, the more alert you felt when you woke up." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Stress", Coefficient = rWakeUpFreshness, Note = "The more stressed you were before bed time, the more dizzy you felt when you woke up." });
                    }
                }
            }
            if (iWakeUpFreshnessTiredness > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessTiredness, tmpTiredness);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Tiredness", Coefficient = rWakeUpFreshness, Note = "The more tired you felt before bed time, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Tiredness", Coefficient = rWakeUpFreshness, Note = "The more tired you felt before bed time, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessDream > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessDream, tmpDream);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Dream", Coefficient = rWakeUpFreshness, Note = "Dreaming makes you feel dizzy when woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Dream", Coefficient = rWakeUpFreshness, Note = "Dreaming makes you feel alert when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessDigitalDev > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessDigitalDev, tmpDigitalDev);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "DigitalDev", Coefficient = rWakeUpFreshness, Note = "The heavier you used digital devices before bed time, the more alert you felt when you woke up." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "DigitalDev", Coefficient = rWakeUpFreshness, Note = "The heavier you used digital devices before bed time, the more dizzy you felt when you woke up." });
                    }
                }
            }
            if (iWakeUpFreshnessLight > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessLight, tmpLight);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Light", Coefficient = rWakeUpFreshness, Note = "The more you were exposed to artificial light before bed time, the more alert you felt when you woke up." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Light", Coefficient = rWakeUpFreshness, Note = "The more you were exposed to artificial light before bed time, the more dizzy you felt when you woke up." });
                    }
                }
            }
            if (iWakeUpFreshnessNapDuration > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessNapDuration, tmpNapDuration);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "NapDuration", Coefficient = rWakeUpFreshness, Note = "The longer nap you took during the day, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "NapDuration", Coefficient = rWakeUpFreshness, Note = "The longer nap you took during the day, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessNapTime > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessNapTime, tmpNapTime);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "NapTime", Coefficient = rWakeUpFreshness, Note = "The earlier you took a nap during the day, the more dizzy you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "NapTime", Coefficient = rWakeUpFreshness, Note = "The earlier you took a nap during the day, the more alert you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessSocialActivity > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessSocialActivity, tmpSocialActivity);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "SocialActivity", Coefficient = rWakeUpFreshness, Note = "The more you were involved in social activities before bed time, the more alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "SocialActivity", Coefficient = rWakeUpFreshness, Note = "The more you were involved in social activities before bed time, the more dizzy you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessDinnerTime > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessDinnerTime, tmpDinnerTime);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "DinnerTime", Coefficient = rWakeUpFreshness, Note = "The earlier you had dinner, the less alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "DinnerTime", Coefficient = rWakeUpFreshness, Note = "The earlier you had dinner, the more alert you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessExerciseTime > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessExerciseTime, tmpExerciseTime);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "ExerciseTime", Coefficient = rWakeUpFreshness, Note = "The earlier you exercised, the less alert you felt when you woke up the second morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "ExerciseTime", Coefficient = rWakeUpFreshness, Note = "The earlier you exercised, the more alert you felt when you woke up the second morning." });
                    }
                }
            }
            if (iWakeUpFreshnessAmbientTemp > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessAmbientTemp, tmpAmbientTemp);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "AmbientTemp", Coefficient = rWakeUpFreshness, Note = "The hotter the environment was, the more alert you felt when you woke up in the morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "AmbientTemp", Coefficient = rWakeUpFreshness, Note = "The hotter the environment was, the less alert you felt when you woke up in the morning." });
                    }
                }
            }
            if (iWakeUpFreshnessAmbientHumd > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessAmbientHumd, tmpAmbientHumd);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "AmbientHumd", Coefficient = rWakeUpFreshness, Note = "The more humid the environment was, the more alert you felt when you woke up in the morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "AmbientHumd", Coefficient = rWakeUpFreshness, Note = "The more humid the environment was, the less alert you felt when you woke up in the morning." });
                    }
                }
            }
            if (iWakeUpFreshnessBodyTemp > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessBodyTemp, tmpBodyTemp);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "BodyTemp", Coefficient = rWakeUpFreshness, Note = "The higher your body temperature was before bed time, the more alert you felt when you woke up in the morning." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "BodyTemp", Coefficient = rWakeUpFreshness, Note = "The higher your body temperature was before bed time, the less alert you felt when you woke up in the morning." });
                    }
                }
            }
            if (iWakeUpFreshnessHormone > 4)
            {
                rWakeUpFreshness = Correlation.Pearson(WakeUpFreshnessHormone, tmpHormone);
                if (Math.Abs(rWakeUpFreshness) >= 0.3)
                {
                    if (rWakeUpFreshness > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Hormone", Coefficient = rWakeUpFreshness, Note = "As the next period approaches, you tend to feel more alert when you woke up." });
                    }
                    else if (rWakeUpFreshness < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "WakeUpFreshness", Name = "Hormone", Coefficient = rWakeUpFreshness, Note = "As the next period approaches, you tend to feel more dizzy when you woke up." });
                    }
                }
            }

            // MinutesAsleep
            double rMinutesSedentary = Correlation.Pearson(MinutesAsleep, MinutesSedentary);
            if (Math.Abs(rMinutesSedentary) >= 0.3)
            {
                if (rMinutesSedentary > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the longer you were asleep during night." });
                }
                else if (rMinutesSedentary < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the shorter you were asleep during night." });
                }
            }

            double rMinutesLightlyActive = Correlation.Pearson(MinutesAsleep, MinutesLightlyActive);
            if (Math.Abs(rMinutesLightlyActive) >= 0.3)
            {
                if (rMinutesLightlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise your did, the longer you were asleep during night." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise your did, the shorter you were asleep during night." });
                }
            }

            double rMinutesFairlyActive = Correlation.Pearson(MinutesAsleep, MinutesFairlyActive);
            if (Math.Abs(rMinutesFairlyActive) >= 0.3)
            {
                if (rMinutesFairlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise your did, the longer you were asleep during night." });
                }
                else if (rMinutesFairlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Minutes Fairly Active", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise your did, the shorter you were asleep during night." });
                }
            }

            double rMinutesVeryActive = Correlation.Pearson(MinutesAsleep, MinutesVeryActive);
            if (Math.Abs(rMinutesVeryActive) >= 0.3)
            {
                if (rMinutesVeryActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Minutes Very Active", Coefficient = rMinutesVeryActive, Note = "The more intense exercise your did, the longer you were asleep during night." });
                }
                else if (rMinutesVeryActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Minutes Very Active", Coefficient = rMinutesVeryActive, Note = "The more intense exercise your did, the shorter you were asleep during night." });
                }
            }


            // MinutesAwake
            rMinutesSedentary = Correlation.Pearson(MinutesAwake, MinutesSedentary);
            if (Math.Abs(rMinutesSedentary) >= 0.3)
            {
                if (rMinutesSedentary > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Sedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the longer you were awake during night." });
                }
                else if (rMinutesSedentary < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Sedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the shorter you were awake during night." });
                }
            }


            rMinutesLightlyActive = Correlation.Pearson(MinutesAwake, MinutesLightlyActive);
            if (Math.Abs(rMinutesLightlyActive) >= 0.3)
            {
                if (rMinutesLightlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Lightly Active", Coefficient = rMinutesLightlyActive, Note = "The more light exercise your did, the longer you were awake during night." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Lightly Active", Coefficient = rMinutesLightlyActive, Note = "The more light exercise your did, the shorter you were awake during night." });
                }
            }

            rMinutesFairlyActive = Correlation.Pearson(MinutesAwake, MinutesFairlyActive);
            if (Math.Abs(rMinutesFairlyActive) >= 0.3)
            {
                if (rMinutesFairlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Fairly Active", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise your did, the longer you were awake during night." });
                }
                else if (rMinutesFairlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Fairly Active", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise your did, the shorter you were awake during night." });
                }
            }

            rMinutesVeryActive = Correlation.Pearson(MinutesAwake, MinutesVeryActive);
            if (Math.Abs(rMinutesVeryActive) >= 0.3)
            {
                if (rMinutesVeryActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Very Active", Coefficient = rMinutesVeryActive, Note = "The more intense exercise your did, the longer you were awake during night." });
                }
                else if (rMinutesVeryActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Minutes Very Active", Coefficient = rMinutesVeryActive, Note = "The more intense exercise your did, the shorter you were awake during night." });
                }
            }


            // AwakeningsCount
            rMinutesSedentary = Correlation.Pearson(AwakeningsCount, MinutesSedentary);
            if (Math.Abs(rMinutesSedentary) >= 0.3)
            {
                if (rMinutesSedentary > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary you were, the more often you were awake during night." });
                }
                else if (rMinutesSedentary < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary you were, the less often you were awake during night." });
                }
            }


            rMinutesLightlyActive = Correlation.Pearson(AwakeningsCount, MinutesLightlyActive);
            if (Math.Abs(rMinutesLightlyActive) >= 0.3)
            {
                if (rMinutesLightlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the more often you were awake during night." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the less often you were awake during night." });
                }
            }

            rMinutesFairlyActive = Correlation.Pearson(AwakeningsCount, MinutesFairlyActive);
            if (Math.Abs(rMinutesFairlyActive) >= 0.3)
            {
                if (rMinutesFairlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the more often you were awake during night." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the less often you were awake during night." });
                }
            }

            rMinutesVeryActive = Correlation.Pearson(AwakeningsCount, MinutesVeryActive);
            if (Math.Abs(rMinutesVeryActive) >= 0.3)
            {
                if (rMinutesVeryActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the more often you were awake during night." });
                }
                else if (rMinutesVeryActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the less often you were awake during night." });
                }
            }


            // MinutesToFallAsleep
            rMinutesSedentary = Correlation.Pearson(MinutesToFallAsleep, MinutesSedentary);
            if (Math.Abs(rMinutesSedentary) >= 0.3)
            {
                if (rMinutesSedentary > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the longer it took you to fall asleep." });
                }
                else if (rMinutesSedentary < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary your were, the faster you fell asleep." });
                }
            }


            rMinutesLightlyActive = Correlation.Pearson(MinutesToFallAsleep, MinutesLightlyActive);
            if (Math.Abs(rMinutesLightlyActive) >= 0.3)
            {
                if (rMinutesLightlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the longer it took you to fall asleep." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the faster you fell asleep." });
                }
            }

            rMinutesFairlyActive = Correlation.Pearson(MinutesToFallAsleep, MinutesFairlyActive);
            if (Math.Abs(rMinutesFairlyActive) >= 0.3)
            {
                if (rMinutesFairlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the longer it took you to fall asleep." });
                }
                else if (rMinutesFairlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the faster you fell asleep." });
                }
            }

            rMinutesVeryActive = Correlation.Pearson(MinutesToFallAsleep, MinutesVeryActive);
            if (Math.Abs(rMinutesVeryActive) >= 0.3)
            {
                if (rMinutesVeryActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the longer it took you to fall asleep." });
                }
                else if (rMinutesVeryActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the faster you fell asleep." });
                }
            }


            // SleepEfficiency
            rMinutesSedentary = Correlation.Pearson(SleepEfficiency, MinutesSedentary);
            if (Math.Abs(rMinutesSedentary) >= 0.3)
            {
                if (rMinutesSedentary > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The more sedentary you were, the better you sleep efficiency was." });
                }
                else if (rMinutesSedentary < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesSedentary", Coefficient = rMinutesSedentary, Note = "The less sedentary you were, the better you sleep efficiency was." });
                }
            }


            rMinutesLightlyActive = Correlation.Pearson(SleepEfficiency, MinutesLightlyActive);
            if (Math.Abs(rMinutesLightlyActive) >= 0.3)
            {
                if (rMinutesLightlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the better you sleep efficiency was." });
                }
                else if (rMinutesLightlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesLightlyActive", Coefficient = rMinutesLightlyActive, Note = "The more light exercise you did, the worse you sleep efficiency was." });
                }
            }

            rMinutesFairlyActive = Correlation.Pearson(SleepEfficiency, MinutesFairlyActive);
            if (Math.Abs(rMinutesFairlyActive) >= 0.3)
            {
                if (rMinutesFairlyActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the better you sleep efficiency was." });
                }
                else if (rMinutesFairlyActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesFairlyActive", Coefficient = rMinutesFairlyActive, Note = "The more moderate exercise you did, the worse you sleep efficiency was." });
                }
            }

            rMinutesVeryActive = Correlation.Pearson(SleepEfficiency, MinutesVeryActive);
            if (Math.Abs(rMinutesVeryActive) >= 0.3)
            {
                if (rMinutesVeryActive > 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the better you sleep efficiency was." });
                }
                else if (rMinutesVeryActive < 0)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "MinutesVeryActive", Coefficient = rMinutesVeryActive, Note = "The more intense exercise you did, the worse you sleep efficiency was." });
                }
            }

            //********** Save changes to DB!!!! ***************
           // db.SaveChanges();

            int temp = 0, identifier = 0;
            double pearson = 0, tempValue = 0;

            // CaloriesIn - DONE!!! YEAH!!!

            if (CNTcaloriesIn > 4)
            {
                double[] CaloriesIn = new double[CNTcaloriesIn];
                double[] tempMinutesAsleepCalariesIn = new double[CNTcaloriesIn];
                double[] tempMinutesAwakeCalariesIn = new double[CNTcaloriesIn];
                double[] tempAwakeningsCountCalariesIn = new double[CNTcaloriesIn];
                double[] tempMinutesToFallAsleepCalariesIn = new double[CNTcaloriesIn];
                double[] tempSleepEfficiencyCalariesIn = new double[CNTcaloriesIn];

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.CaloriesIn);
                    if (tempValue > 0)
                    {
                        CaloriesIn[temp] = tempValue;
                        tempMinutesAsleepCalariesIn[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeCalariesIn[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountCalariesIn[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepCalariesIn[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencyCalariesIn[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepCalariesIn, CaloriesIn);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeCalariesIn, CaloriesIn);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountCalariesIn, CaloriesIn);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the more often were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the less often were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepCalariesIn, CaloriesIn);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyCalariesIn, CaloriesIn);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CaloriesIn", Coefficient = pearson, Note = "The more calories you intook, the worse your sleep efficiency was." });
                    }
                }

            }

            // CaloriesOut -- DONE !!! YEAH !!!

            if (CNTcaloriesOut > 4)
            {
                double[] CaloriesOut = new double[CNTcaloriesOut];
                double[] tempMinutesAsleepCalariesOut = new double[CNTcaloriesOut];
                double[] tempMinutesAwakeCalariesOut = new double[CNTcaloriesOut];
                double[] tempAwakeningsCountCalariesOut = new double[CNTcaloriesOut];
                double[] tempMinutesToFallAsleepCalariesOut = new double[CNTcaloriesOut];
                double[] tempSleepEfficiencyCalariesOut = new double[CNTcaloriesOut];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.CaloriesOut);
                    if (tempValue > 0)
                    {
                        CaloriesOut[temp] = tempValue;
                        tempMinutesAsleepCalariesOut[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeCalariesOut[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountCalariesOut[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepCalariesOut[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencyCalariesOut[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepCalariesOut, CaloriesOut);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeCalariesOut, CaloriesOut);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountCalariesOut, CaloriesOut);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepCalariesOut, CaloriesOut);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyCalariesOut, CaloriesOut);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the better you sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CaloriesOut", Coefficient = pearson, Note = "The more calories you output, the worse you sleep efficiency was." });
                    }
                }

            }

            // Water -- DONE YEAH!!

            if (CNTwater > 4)
            {
                double[] Water = new double[CNTwater];
                double[] tempMinutesAsleepWater = new double[CNTwater];
                double[] tempMinutesAwakeWater = new double[CNTwater];
                double[] tempAwakeningsCountWater = new double[CNTwater];
                double[] tempMinutesToFallAsleepWater = new double[CNTwater];
                double[] tempSleepEfficiencyWater = new double[CNTwater];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.Water);
                    if (tempValue > 0)
                    {
                        Water[temp] = tempValue;
                        tempMinutesAsleepWater[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeWater[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountWater[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepWater[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencyWater[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepWater, Water);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeWater, Water);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountWater, Water);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepWater, Water);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyWater, Water);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Water", Coefficient = pearson, Note = "The more water you drunk, the worse your sleep efficiency was." });
                    }
                }

            }

            // Steps -- DONE YEAH!!

            if (CNTsteps > 4)
            {
                double[] Steps = new double[CNTsteps];
                double[] tempMinutesAsleepSteps = new double[CNTsteps];
                double[] tempMinutesAwakeSteps = new double[CNTsteps];
                double[] tempAwakeningsCountSteps = new double[CNTsteps];
                double[] tempMinutesToFallAsleepSteps = new double[CNTsteps];
                double[] tempSleepEfficiencySteps = new double[CNTsteps];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.Steps);
                    if (tempValue > 0)
                    {
                        Steps[temp] = tempValue;
                        tempMinutesAsleepSteps[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeSteps[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountSteps[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepSteps[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencySteps[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepSteps, Steps);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeSteps, Steps);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountSteps, Steps);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepSteps, Steps);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencySteps, Steps);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Steps", Coefficient = pearson, Note = "The more steps you walked, the worse your sleep efficiency was." });
                    }
                }

            }

            // weight -- DONE YEAH!!

            if (CNTweight > 4)
            {
                double[] Weight = new double[CNTweight];
                double[] tempMinutesAsleepWeight = new double[CNTweight];
                double[] tempMinutesAwakeWeight = new double[CNTweight];
                double[] tempAwakeningsCountWeight = new double[CNTweight];
                double[] tempMinutesToFallAsleepWeight = new double[CNTweight];
                double[] tempSleepEfficiencyWeight = new double[CNTweight];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.Weight);
                    if (tempValue > 0)
                    {
                        Weight[temp] = tempValue;
                        tempMinutesAsleepWeight[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeWeight[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountWeight[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepWeight[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencyWeight[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepWeight, Weight);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeWeight, Weight);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountWeight, Weight);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepWeight, Weight);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyWeight, Weight);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Weight", Coefficient = pearson, Note = "The heavier your weight was, the worse your sleep efficiency was." });
                    }
                }

            }

            // Fat -- DONE YEAH!!

            if (CNTfat > 4)
            {
                double[] Fat = new double[CNTfat];
                double[] tempMinutesAsleepFat = new double[CNTfat];
                double[] tempMinutesAwakeFat = new double[CNTfat];
                double[] tempAwakeningsCountFat = new double[CNTfat];
                double[] tempMinutesToFallAsleepFat = new double[CNTfat];
                double[] tempSleepEfficiencyFat = new double[CNTfat];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    tempValue = Convert.ToDouble(item.Fat);
                    if (tempValue > 0)
                    {
                        Fat[temp] = tempValue;
                        tempMinutesAsleepFat[temp] = MinutesAsleep[identifier];
                        tempMinutesAwakeFat[temp] = MinutesAwake[identifier];
                        tempAwakeningsCountFat[temp] = AwakeningsCount[identifier];
                        tempMinutesToFallAsleepFat[temp] = MinutesToFallAsleep[identifier];
                        tempSleepEfficiencyFat[temp] = SleepEfficiency[identifier];

                        temp++;
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepFat, Fat);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Fat", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeFat, Fat);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Fat", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountFat, Fat);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Fat", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepFat, Fat);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Fat", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyFat, Fat);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Fat", Coefficient = pearson });
                }

            }



            // Coffee -- DONE YEAH!!

            if (CNTcoffee > 4)
            {
                double[] Coffee = new double[CNTcoffee];
                double[] tempMinutesAsleepCoffee = new double[CNTcoffee];
                double[] tempMinutesAwakeCoffee = new double[CNTcoffee];
                double[] tempAwakeningsCountCoffee = new double[CNTcoffee];
                double[] tempMinutesToFallAsleepCoffee = new double[CNTcoffee];
                double[] tempSleepEfficiencyCoffee = new double[CNTcoffee];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Coffee != null)
                    {
                        tempValue = Convert.ToDouble(item.Coffee);
                        if (tempValue >= 0)
                        {
                            Coffee[temp] = tempValue;
                            tempMinutesAsleepCoffee[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeCoffee[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountCoffee[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepCoffee[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyCoffee[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepCoffee, Coffee);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeCoffee, Coffee);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountCoffee, Coffee);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepCoffee, Coffee);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyCoffee, Coffee);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Coffee", Coefficient = pearson, Note = "The more coffee you consumed during the day, the worse your sleep efficiency was." });
                    }
                }

            }



            // CoffeeTime -- DONE YEAH!!

            if (CNTcoffeeTime > 4)
            {
                double[] CoffeeTime = new double[CNTcoffeeTime];
                double[] tempMinutesAsleepCoffeeTime = new double[CNTcoffeeTime];
                double[] tempMinutesAwakeCoffeeTime = new double[CNTcoffeeTime];
                double[] tempAwakeningsCountCoffeeTime = new double[CNTcoffeeTime];
                double[] tempMinutesToFallAsleepCoffeeTime = new double[CNTcoffeeTime];
                double[] tempSleepEfficiencyCoffeeTime = new double[CNTcoffeeTime];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.CoffeeTime != null)
                    {
                        tempValue = Convert.ToDouble(item.CoffeeTime);
                        if (tempValue > 0)
                        {
                            CoffeeTime[temp] = tempValue;
                            tempMinutesAsleepCoffeeTime[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeCoffeeTime[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountCoffeeTime[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepCoffeeTime[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyCoffeeTime[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepCoffeeTime, CoffeeTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (Math.Abs(pearson) >= 0.3)
                    {
                        if (pearson > 0)
                        {
                            CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the shorter time you were asleep during night." });
                        }
                        else if (pearson < 0)
                        {
                            CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the longer time you were asleep during night." });
                        }
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeCoffeeTime, CoffeeTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the shorter time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the longer time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountCoffeeTime, CoffeeTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the less often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the more often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepCoffeeTime, CoffeeTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the faster you fell asleep at night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the longer it took you to fall asleep at night." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyCoffeeTime, CoffeeTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the worse your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "CoffeeTime", Coefficient = pearson, Note = "The earlier you drunk coffee, the better your sleep efficiency was." });
                    }
                }

            }

            // Alcohol -- DONE YEAH!!

            if (CNTalcohol > 4)
            {
                double[] Alcohol = new double[CNTalcohol];
                double[] tempMinutesAsleepAlcohol = new double[CNTalcohol];
                double[] tempMinutesAwakeAlcohol = new double[CNTalcohol];
                double[] tempAwakeningsCountAlcohol = new double[CNTalcohol];
                double[] tempMinutesToFallAsleepAlcohol = new double[CNTalcohol];
                double[] tempSleepEfficiencyAlcohol = new double[CNTalcohol];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Alcohol != null)
                    {
                        tempValue = Convert.ToDouble(item.Alcohol);
                        if (tempValue >= 0)
                        {
                            Alcohol[temp] = tempValue;
                            tempMinutesAsleepAlcohol[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeAlcohol[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountAlcohol[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepAlcohol[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyAlcohol[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }

                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepAlcohol, Alcohol);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the longer time you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the shorter time you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeAlcohol, Alcohol);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the longer time you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the shorter time you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountAlcohol, Alcohol);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the more often you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the less often you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepAlcohol, Alcohol);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyAlcohol, Alcohol);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Alcohol", Coefficient = pearson, Note = "The more alcohol you consumed, the worse your sleep efficiency was." });
                    }
                }

            }


            // Mood -- DONE YEAH!!

            if (CNTmood > 4)
            {
                double[] Mood = new double[CNTmood];
                double[] tempMinutesAsleepMood = new double[CNTmood];
                double[] tempMinutesAwakeMood = new double[CNTmood];
                double[] tempAwakeningsCountMood = new double[CNTmood];
                double[] tempMinutesToFallAsleepMood = new double[CNTmood];
                double[] tempSleepEfficiencyMood = new double[CNTmood];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Mood != null)
                    {
                        tempValue = Convert.ToDouble(item.Mood);
                        if (tempValue >= 0)
                        {
                            Mood[temp] = tempValue;
                            tempMinutesAsleepMood[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeMood[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountMood[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepMood[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyMood[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepMood, Mood);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the more minutes you were asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the less minutes you were asleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeMood, Mood);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the more minutes you were awake during sleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the less minutes you were awake during sleep." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountMood, Mood);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the more often you were awake during sleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the less often you were awake during sleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepMood, Mood);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyMood, Mood);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Mood", Coefficient = pearson, Note = "The happier you felt before bed time, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Mood", Coefficient = pearson, Note = "Oops! The happier you felt before bed time, the worse your sleep efficiency was." });
                    }
                }

            }


            // Stress -- DONE YEAH!!

            if (CNTstress > 4)
            {
                double[] Stress = new double[CNTstress];
                double[] tempMinutesAsleepStress = new double[CNTstress];
                double[] tempMinutesAwakeStress = new double[CNTstress];
                double[] tempAwakeningsCountStress = new double[CNTstress];
                double[] tempMinutesToFallAsleepStress = new double[CNTstress];
                double[] tempSleepEfficiencyStress = new double[CNTstress];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Stress != null)
                    {
                        tempValue = Convert.ToDouble(item.Stress);
                        if (tempValue >= 0)
                        {
                            Stress[temp] = tempValue;
                            tempMinutesAsleepStress[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeStress[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountStress[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepStress[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyStress[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepStress, Stress);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the more minutes you were asleep during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the less minutes you were asleep during night." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeStress, Stress);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the more minutes you were awake during night." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the less minutes you were awake during night." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountStress, Stress);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the more awakenings you had." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the less awakenings you had." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepStress, Stress);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyStress, Stress);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Stress", Coefficient = pearson, Note = "The more stressed you were, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Stress", Coefficient = pearson, Note = "The less stressed you were, the better your sleep efficiency was." });
                    }
                }

            }

            // Tiredness -- DONE YEAH!!

            if (CNTtiredness > 4)
            {
                double[] Tiredness = new double[CNTtiredness];
                double[] tempMinutesAsleepTiredness = new double[CNTtiredness];
                double[] tempMinutesAwakeTiredness = new double[CNTtiredness];
                double[] tempAwakeningsCountTiredness = new double[CNTtiredness];
                double[] tempMinutesToFallAsleepTiredness = new double[CNTtiredness];
                double[] tempSleepEfficiencyTiredness = new double[CNTtiredness];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Tiredness != null)
                    {
                        tempValue = Convert.ToDouble(item.Tiredness);
                        if (tempValue >= 0)
                        {
                            Tiredness[temp] = tempValue;
                            tempMinutesAsleepTiredness[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeTiredness[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountTiredness[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepTiredness[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyTiredness[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepTiredness, Tiredness);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the more minutes you were asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the less minutes you were asleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeTiredness, Tiredness);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the more minutes you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the less minutes you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountTiredness, Tiredness);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the more often you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the less often you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepTiredness, Tiredness);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyTiredness, Tiredness);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Tiredness", Coefficient = pearson, Note = "The more tired you were before bed time, the worse your sleep efficiency was." });
                    }
                }

            }



            // Dream -- DONE YEAH!!

            if (CNTdream > 4)
            {
                double[] Dream = new double[CNTdream];
                double[] tempMinutesAsleepDream = new double[CNTdream];
                double[] tempMinutesAwakeDream = new double[CNTdream];
                double[] tempAwakeningsCountDream = new double[CNTdream];
                double[] tempMinutesToFallAsleepDream = new double[CNTdream];
                double[] tempSleepEfficiencyDream = new double[CNTdream];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Dream != null)
                    {
                        tempValue = Convert.ToDouble(item.Dream);
                        if (tempValue >= 0)
                        {
                            Dream[temp] = tempValue;
                            tempMinutesAsleepDream[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeDream[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountDream[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepDream[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyDream[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepDream, Dream);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the less minutes you were asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the more minutes you were asleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeDream, Dream);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the less minutes you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the more minutes you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountDream, Dream);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the less often you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the more often you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepDream, Dream);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the faster you fell asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the longer it took you to fall asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyDream, Dream);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the worse your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Dream", Coefficient = pearson, Note = "The more you dreamed, the better your sleep efficiency was." });
                    }
                }

            }

            // DigitalDev -- DONE YEAH!!

            if (CNTdigitalDev > 4)
            {
                double[] DigitalDev = new double[CNTdigitalDev];
                double[] tempMinutesAsleepDigitalDev = new double[CNTdigitalDev];
                double[] tempMinutesAwakeDigitalDev = new double[CNTdigitalDev];
                double[] tempAwakeningsCountDigitalDev = new double[CNTdigitalDev];
                double[] tempMinutesToFallAsleepDigitalDev = new double[CNTdigitalDev];
                double[] tempSleepEfficiencyDigitalDev = new double[CNTdigitalDev];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.DigitalDev != null)
                    {
                        tempValue = Convert.ToDouble(item.DigitalDev);
                        if (tempValue >= 0)
                        {
                            DigitalDev[temp] = tempValue;
                            tempMinutesAsleepDigitalDev[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeDigitalDev[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountDigitalDev[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepDigitalDev[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyDigitalDev[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepDigitalDev, DigitalDev);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the more minutes you were asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the less minutes you were asleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeDigitalDev, DigitalDev);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the more minutes you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the less minutes you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountDigitalDev, DigitalDev);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the more often you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the less often you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepDigitalDev, DigitalDev);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyDigitalDev, DigitalDev);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "DigitalDevices", Coefficient = pearson, Note = "The heavier you used digital devices before bed time, the worse your sleep efficiency was." });
                    }
                }

            }


            // Light -- DONE YEAH!!

            if (CNTlight > 4)
            {
                double[] Light = new double[CNTlight];
                double[] tempMinutesAsleepLight = new double[CNTlight];
                double[] tempMinutesAwakeLight = new double[CNTlight];
                double[] tempAwakeningsCountLight = new double[CNTlight];
                double[] tempMinutesToFallAsleepLight = new double[CNTlight];
                double[] tempSleepEfficiencyLight = new double[CNTlight];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Light != null)
                    {
                        tempValue = Convert.ToDouble(item.Light);
                        if (tempValue >= 0)
                        {
                            Light[temp] = tempValue;
                            tempMinutesAsleepLight[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeLight[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountLight[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepLight[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyLight[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepLight, Light);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the more minutes you were asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the less minutes you were asleep." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeLight, Light);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the more minutes you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the less minutes you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountLight, Light);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the more often you were awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the less often you were awake." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepLight, Light);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the longer it took you to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the faster you fell asleep." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyLight, Light);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the better your sleep efficiency was." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Light", Coefficient = pearson, Note = "The more you were exposed to artificial light before bed time, the worse your sleep efficiency was." });
                    }
                }

            }


            // NapDuration -- DONE YEAH!!

            if (CNTnapDuration > 4)
            {
                double[] NapDuration = new double[CNTnapDuration];
                double[] tempMinutesAsleepNapDuration = new double[CNTnapDuration];
                double[] tempMinutesAwakeNapDuration = new double[CNTnapDuration];
                double[] tempAwakeningsCountNapDuration = new double[CNTnapDuration];
                double[] tempMinutesToFallAsleepNapDuration = new double[CNTnapDuration];
                double[] tempSleepEfficiencyNapDuration = new double[CNTnapDuration];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.NapDuration != null)
                    {
                        tempValue = Convert.ToDouble(item.NapDuration);
                        if (tempValue >= 0)
                        {
                            NapDuration[temp] = tempValue;
                            tempMinutesAsleepNapDuration[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeNapDuration[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountNapDuration[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepNapDuration[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyNapDuration[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepNapDuration, NapDuration);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "NapDuration", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeNapDuration, NapDuration);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "NapDuration", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountNapDuration, NapDuration);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "NapDuration", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepNapDuration, NapDuration);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "NapDuration", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyNapDuration, NapDuration);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "NapDuration", Coefficient = pearson });
                }

            }


            // NapTime -- DONE YEAH!!

            if (CNTnapTime > 4)
            {
                double[] NapTime = new double[CNTnapTime];
                double[] tempMinutesAsleepNapTime = new double[CNTnapTime];
                double[] tempMinutesAwakeNapTime = new double[CNTnapTime];
                double[] tempAwakeningsCountNapTime = new double[CNTnapTime];
                double[] tempMinutesToFallAsleepNapTime = new double[CNTnapTime];
                double[] tempSleepEfficiencyNapTime = new double[CNTnapTime];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.NapTime != null)
                    {
                        tempValue = Convert.ToDouble(item.NapTime);
                        if (tempValue > 0)
                        {
                            NapTime[temp] = tempValue;
                            tempMinutesAsleepNapTime[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeNapTime[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountNapTime[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepNapTime[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyNapTime[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepNapTime, NapTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "NapTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeNapTime, NapTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "NapTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountNapTime, NapTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "NapTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepNapTime, NapTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "NapTime", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyNapTime, NapTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "NapTime", Coefficient = pearson });
                }

            }

            // SocialActivity -- DONE YEAH!!

            if (CNTsocialActivity > 4)
            {
                double[] SocialActivity = new double[CNTsocialActivity];
                double[] tempMinutesAsleepSocialActivity = new double[CNTsocialActivity];
                double[] tempMinutesAwakeSocialActivity = new double[CNTsocialActivity];
                double[] tempAwakeningsCountSocialActivity = new double[CNTsocialActivity];
                double[] tempMinutesToFallAsleepSocialActivity = new double[CNTsocialActivity];
                double[] tempSleepEfficiencySocialActivity = new double[CNTsocialActivity];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.SocialActivity != null)
                    {
                        tempValue = Convert.ToDouble(item.SocialActivity);
                        if (tempValue >= 0)
                        {
                            SocialActivity[temp] = tempValue;
                            tempMinutesAsleepSocialActivity[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeSocialActivity[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountSocialActivity[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepSocialActivity[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencySocialActivity[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepSocialActivity, SocialActivity);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "SocialActivity", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeSocialActivity, SocialActivity);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "SocialActivity", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountSocialActivity, SocialActivity);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "SocialActivity", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepSocialActivity, SocialActivity);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "SocialActivity", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencySocialActivity, SocialActivity);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "SocialActivity", Coefficient = pearson });
                }

            }

            // DinnerTime -- DONE YEAH!!

            if (CNTdinnerTime > 4)
            {
                double[] DinnerTime = new double[CNTdinnerTime];
                double[] tempMinutesAsleepDinnerTime = new double[CNTdinnerTime];
                double[] tempMinutesAwakeDinnerTime = new double[CNTdinnerTime];
                double[] tempAwakeningsCountDinnerTime = new double[CNTdinnerTime];
                double[] tempMinutesToFallAsleepDinnerTime = new double[CNTdinnerTime];
                double[] tempSleepEfficiencyDinnerTime = new double[CNTdinnerTime];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.DinnerTime != null)
                    {
                        tempValue = Convert.ToDouble(item.DinnerTime);
                        if (tempValue > 0)
                        {
                            DinnerTime[temp] = tempValue;
                            tempMinutesAsleepDinnerTime[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeDinnerTime[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountDinnerTime[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepDinnerTime[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyDinnerTime[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepDinnerTime, DinnerTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "DinnerTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeDinnerTime, DinnerTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "DinnerTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountDinnerTime, DinnerTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "DinnerTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepDinnerTime, DinnerTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "DinnerTime", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyDinnerTime, DinnerTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "DinnerTime", Coefficient = pearson });
                }

            }

            // ExerciseTime -- DONE YEAH!!

            if (CNTexerciseTime > 4)
            {
                double[] ExerciseTime = new double[CNTexerciseTime];
                double[] tempMinutesAsleepExerciseTime = new double[CNTexerciseTime];
                double[] tempMinutesAwakeExerciseTime = new double[CNTexerciseTime];
                double[] tempAwakeningsCountExerciseTime = new double[CNTexerciseTime];
                double[] tempMinutesToFallAsleepExerciseTime = new double[CNTexerciseTime];
                double[] tempSleepEfficiencyExerciseTime = new double[CNTexerciseTime];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.ExerciseTime != null)
                    {
                        tempValue = Convert.ToDouble(item.ExerciseTime);
                        if (tempValue > 0)
                        {
                            ExerciseTime[temp] = tempValue;
                            tempMinutesAsleepExerciseTime[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeExerciseTime[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountExerciseTime[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepExerciseTime[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyExerciseTime[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepExerciseTime, ExerciseTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "ExerciseTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeExerciseTime, ExerciseTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "ExerciseTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountExerciseTime, ExerciseTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "ExerciseTime", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepExerciseTime, ExerciseTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "ExerciseTime", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyExerciseTime, ExerciseTime);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "ExerciseTime", Coefficient = pearson });
                }

            }


            // AmbientTemp -- DONE YEAH!!

            if (CNTambientTemp > 4)
            {
                double[] AmbientTemp = new double[CNTambientTemp];
                double[] tempMinutesAsleepAmbientTemp = new double[CNTambientTemp];
                double[] tempMinutesAwakeAmbientTemp = new double[CNTambientTemp];
                double[] tempAwakeningsCountAmbientTemp = new double[CNTambientTemp];
                double[] tempMinutesToFallAsleepAmbientTemp = new double[CNTambientTemp];
                double[] tempSleepEfficiencyAmbientTemp = new double[CNTambientTemp];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.AmbientTemp != null)
                    {
                        tempValue = Convert.ToDouble(item.AmbientTemp);
                        if (tempValue > 0)
                        {
                            AmbientTemp[temp] = tempValue;
                            tempMinutesAsleepAmbientTemp[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeAmbientTemp[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountAmbientTemp[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepAmbientTemp[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyAmbientTemp[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepAmbientTemp, AmbientTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "AmbientTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeAmbientTemp, AmbientTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "AmbientTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountAmbientTemp, AmbientTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "AmbientTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepAmbientTemp, AmbientTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "AmbientTemp", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyAmbientTemp, AmbientTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "AmbientTemp", Coefficient = pearson });
                }

            }


            // AmbientHumd -- DONE YEAH!!

            if (CNTambientHumd > 4)
            {
                double[] AmbientHumd = new double[CNTambientHumd];
                double[] tempMinutesAsleepAmbientHumd = new double[CNTambientHumd];
                double[] tempMinutesAwakeAmbientHumd = new double[CNTambientHumd];
                double[] tempAwakeningsCountAmbientHumd = new double[CNTambientHumd];
                double[] tempMinutesToFallAsleepAmbientHumd = new double[CNTambientHumd];
                double[] tempSleepEfficiencyAmbientHumd = new double[CNTambientHumd];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.AmbientHumd != null)
                    {
                        tempValue = Convert.ToDouble(item.AmbientHumd);
                        if (tempValue > 0)
                        {
                            AmbientHumd[temp] = tempValue;
                            tempMinutesAsleepAmbientHumd[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeAmbientHumd[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountAmbientHumd[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepAmbientHumd[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyAmbientHumd[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepAmbientHumd, AmbientHumd);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "AmbientHumd", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeAmbientHumd, AmbientHumd);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "AmbientHumd", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountAmbientHumd, AmbientHumd);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "AmbientHumd", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepAmbientHumd, AmbientHumd);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "AmbientHumd", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyAmbientHumd, AmbientHumd);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "AmbientHumd", Coefficient = pearson });
                }

            }


            // BodyTemp -- DONE YEAH!!

            if (CNTbodyTemp > 4)
            {
                double[] BodyTemp = new double[CNTbodyTemp];
                double[] tempMinutesAsleepBodyTemp = new double[CNTbodyTemp];
                double[] tempMinutesAwakeBodyTemp = new double[CNTbodyTemp];
                double[] tempAwakeningsCountBodyTemp = new double[CNTbodyTemp];
                double[] tempMinutesToFallAsleepBodyTemp = new double[CNTbodyTemp];
                double[] tempSleepEfficiencyBodyTemp = new double[CNTbodyTemp];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.BodyTemp != null)
                    {
                        tempValue = Convert.ToDouble(item.BodyTemp);
                        if (tempValue > 0)
                        {
                            BodyTemp[temp] = tempValue;
                            tempMinutesAsleepBodyTemp[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeBodyTemp[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountBodyTemp[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepBodyTemp[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyBodyTemp[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepBodyTemp, BodyTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "BodyTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesAwakeBodyTemp, BodyTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "BodyTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempAwakeningsCountBodyTemp, BodyTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "BodyTemp", Coefficient = pearson });
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepBodyTemp, BodyTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "BodyTemp", Coefficient = pearson });
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyBodyTemp, BodyTemp);
                if (Math.Abs(pearson) >= 0.3)
                {
                    CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "BodyTemp", Coefficient = pearson });
                }

            }


            // Hormone -- DONE YEAH!!

            if (CNThormone > 4)
            {
                double[] Hormone = new double[CNThormone];
                double[] tempMinutesAsleepHormone = new double[CNThormone];
                double[] tempMinutesAwakeHormone = new double[CNThormone];
                double[] tempAwakeningsCountHormone = new double[CNThormone];
                double[] tempMinutesToFallAsleepHormone = new double[CNThormone];
                double[] tempSleepEfficiencyHormone = new double[CNThormone];

                // counters back to zero
                temp = 0;
                identifier = 0;

                foreach (SleepMakeSense.Models.Userdata item in results)
                {
                    if (item.Hormone != null)
                    {
                        tempValue = Convert.ToDouble(item.Hormone);
                        if (tempValue > 0)
                        {
                            Hormone[temp] = tempValue;
                            tempMinutesAsleepHormone[temp] = MinutesAsleep[identifier];
                            tempMinutesAwakeHormone[temp] = MinutesAwake[identifier];
                            tempAwakeningsCountHormone[temp] = AwakeningsCount[identifier];
                            tempMinutesToFallAsleepHormone[temp] = MinutesToFallAsleep[identifier];
                            tempSleepEfficiencyHormone[temp] = SleepEfficiency[identifier];

                            temp++;
                        }
                    }
                    identifier++;
                }

                pearson = Correlation.Pearson(tempMinutesAsleepHormone, Hormone);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have longer time asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAsleep", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have shorter time asleep.." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesAwakeHormone, Hormone);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have longer time awake." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesAwake", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have shorter time awake." });
                    }
                }

                pearson = Correlation.Pearson(tempAwakeningsCountHormone, Hormone);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have more awakenings." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "AwakeningsCount", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to have less awakenings." });
                    }
                }

                pearson = Correlation.Pearson(tempMinutesToFallAsleepHormone, Hormone);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, it took you longer to fall asleep." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "MinutesToFallAsleep", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, you tend to fall asleep faster." });
                    }
                }


                pearson = Correlation.Pearson(tempSleepEfficiencyHormone, Hormone);
                if (Math.Abs(pearson) >= 0.3)
                {
                    if (pearson > 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, your sleep efficiency tends to become better." });
                    }
                    else if (pearson < 0)
                    {
                        CoefficientList.Add(new CorrList() { Belong = "SleepEfficiency", Name = "Hormone", Coefficient = pearson, Note = "As the next period approaches, your sleep efficiency tends to become worse." });
                    }
                }

            }

            var model = new MyViewModel();
            model.AllData = results;
            model.CorrCoefficient = CoefficientList;

            return model;

        }   
    }    
}

