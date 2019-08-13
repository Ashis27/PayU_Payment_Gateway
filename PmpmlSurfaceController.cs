using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using System.Configuration;
using Newtonsoft.Json;
using System.Net.Mail;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence;
using System.Reflection;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.ComponentModel;
using pmpml.Infrastructure;
using System.Collections.Specialized;
using iTextSharp.text.pdf;
using pmpml.Model;
using System.Net.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Net.Http.Headers;
using System.Web.Security;

namespace pmpml.Controllers
{
    public class PmpmlSurfaceController : SurfaceController
    {
        private PmpmlDbContext dbContext = new PmpmlDbContext();
        private string action1 = string.Empty;
        private string hash1 = string.Empty;
        private string txnid1 = string.Empty;
        ILogger _logger;

        public PmpmlSurfaceController()
        {
            //_logger = logger;
        }
        //[HttpGet]
        //public void Home()
        //{
        //     Response.Redirect(System.Web.HttpContext.Current.Request.Url.Authority + "/employee-corner-login");
        //  //  return Redirect(System.Web.HttpContext.Current.Request.Url.Authority + "/employee-corner-login");
        //}

        [HttpPost]
        public ActionResult SaveFeedbackAndSuggestionDetails()
        {
            PMPMLFeedBack feedbackDetails = JsonConvert.DeserializeObject<PMPMLFeedBack>(Request.Form["feedbackFormDetails"]);
            PMPMLFeedBack obj = new PMPMLFeedBack();
            obj.FirstName = feedbackDetails.FirstName;
            obj.LastName = feedbackDetails.LastName;
            obj.Email = feedbackDetails.Email;
            obj.ContactNum = feedbackDetails.ContactNum;
            obj.Category = feedbackDetails.Category != null ? feedbackDetails.Category : "";
            //  obj.SubCategory = feedbackDetails.SubCategory;
            obj.Suggestion = feedbackDetails.Suggestion;
            //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
            UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            //obj.CreatedBy = "";
            db.Insert(obj);
            Crypto crypto = new Crypto(Crypto.CryptoTypes.encTypeTripleDES);
            string from = ConfigurationManager.AppSettings["FeedbackFromEmail"];
            string password = ConfigurationManager.AppSettings["FeedbackFromPassword"];//crypto.Decrypt(ConfigurationManager.AppSettings["FromPassword"]);
            string mailTo = ConfigurationManager.AppSettings["FeedbackMailReceivers"];
            mailTo = mailTo + "," + feedbackDetails.Email;


            using (MailMessage mail = new MailMessage(from, mailTo))
            {
                mail.Subject = "Feedback";
                //mail.Body = enquiryDetails.UserName + " having " + enquiryDetails.UserEmail + " has recently contacted " + " for " + mail.Subject + " Please check and revert back.";
                mail.Body = String.Format("<span style='font-family:Verdana; font-size:11px;'>Dear {0},<br/>" +
                    "We have Received your Valuable Feedback.Please find the submitted Feed back Details.<br/>" +
                    "Thanks,<br/>" +
                    "Team PMPML</span><br/><br/><table style='width: 450px'>" +
        "<tr>" +
            "<td colspan='2' style='text-align:center; font-family:Verdana; font-size:11px; font-weight:bold'>Feedback Details</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>First Name</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{1}</td>" +
        "</tr>" +
            "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>Last Name</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{2}</td>" +
        "</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Email</td>" +
            "<td style='font-family:Verdana; font-size:10px;'>{3}</td>" +
        "</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Contact No.</td>" +
            "<td style='font-family:Verdana; font-size:10px;'>{4}</td>" +
        "</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Category</td>" +
            "<td style='font-family:Verdana; font-size:10px;'>{5}</td>" +
        "</tr>" +
        // "<tr>" +
        //    "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Subcategory</td>" +
        //    "<td style='font-family:Verdana; font-size:10px;'>{6}</td>" +
        //"</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Suggestion</td>" +
            "<td style='font-family:Verdana; font-size:10px;'>{6}</td>" +
        "</tr>" +

    "</table>", feedbackDetails.FirstName + " " + feedbackDetails.LastName, feedbackDetails.FirstName, feedbackDetails.LastName, feedbackDetails.Email, feedbackDetails.ContactNum, feedbackDetails.Category, feedbackDetails.Suggestion);

                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["MailSMTP"];
                smtp.Port = int.Parse(ConfigurationManager.AppSettings["Mailport"]);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(from, password);
                smtp.EnableSsl = false;
                try
                {
                    smtp.Send(mail);
                }
                catch (SmtpException e)
                {
                    return Json(new { Result = "Something error, Please Contact support.", Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { Result = "", Message = "Success" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveGreivanceFormDetails()
        {
            UmbracoGreivance greivanceFormDetails = JsonConvert.DeserializeObject<UmbracoGreivance>(Request.Form["greivanceFormDetails"]);
            UmbracoGreivance obj = new UmbracoGreivance();
            obj.Name = greivanceFormDetails.Name;
            obj.Title = greivanceFormDetails.Title;
            obj.Date = greivanceFormDetails.Date;
            obj.HomeMailAddress = greivanceFormDetails.HomeMailAddress;
            obj.WorkMailAddress = greivanceFormDetails.WorkMailAddress;
            obj.AccountDetails = greivanceFormDetails.AccountDetails;
            obj.GreivancePurpose = greivanceFormDetails.GreivancePurpose;
            obj.EmployeeId = greivanceFormDetails.EmployeeId;
            obj.CreatedBy = "";
            obj.CreatedDate = DateTime.Now;
            //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
            UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            db.Insert(obj);

            //------Send MAIL------

            //        Crypto crypto = new Crypto(Crypto.CryptoTypes.encTypeTripleDES);
            //        string from = ConfigurationManager.AppSettings["FromEmail"];
            //        string password = crypto.Decrypt(ConfigurationManager.AppSettings["FromPassword"]);
            //        string mailTo = ConfigurationManager.AppSettings["MailReceivers"];
            //        //mailTo = mailTo + "," + contactUsDetails.UserLogin;
            //        using (MailMessage mail = new MailMessage(from, mailTo))
            //        {
            //            mail.Subject = ConfigurationManager.AppSettings["ContactSubject"];
            //            //mail.Body = enquiryDetails.UserName + " having " + enquiryDetails.UserEmail + " has recently contacted " + " for " + mail.Subject + " Please check and revert back.";
            //            mail.Body = String.Format("<span style='font-family:Verdana; font-size:11px;'>Dear {0},<br/>" +
            //                "You have a new Request from {1} via the <a href='http://54.84.255.41:8087/'>Kare4u Consumer Portal</a>. Please find the details below.<br/><br/>" +
            //                "Thanks,<br/>" +
            //                "Team Kare4U</span><br/><br/><table style='width: 450px'>" +
            //    "<tr>" +
            //        "<td colspan='2' style='text-align:center; font-family:Verdana; font-size:11px; font-weight:bold'>Hospital Details</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>Hospital Name</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{2}</td>" +
            //    "</tr>" +
            //        "<tr>" +
            //        "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>Hospital URL</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{3}</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Name</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{4}</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Email</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{5}</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Contact No</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{6}</td>" +
            //    "</tr>" +
            //     "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Interested In Plan</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{7}</td>" +
            //    "</tr>" +
            //    "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Selected Services</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{8}</td>" +
            //    "</tr>" +
            //      "<tr>" + +
            //    "</tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Interested In Mobile App</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{9}</td>"
            //     "<tr>" +
            //        "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Your Current Focus</td>" +
            //        "<td style='font-family:Verdana; font-size:10px;'>{10}</td>" +
            //    "</tr>" +
            //"</table>", ConfigurationManager.AppSettings["MailToName"], contactUsDetails.HospitalName, contactUsDetails.HospitalName, contactUsDetails.HospitalUrl, contactUsDetails.ConsumerName, contactUsDetails.UserLogin, contactUsDetails.Contact, contactUsDetails.InterestedInPlan, contactUsDetails.SelectedServices, contactUsDetails.IntertestiInMobile, contactUsDetails.CurrentFocus);

            //            mail.IsBodyHtml = true;

            //            SmtpClient smtp = new SmtpClient();
            //            smtp.Host = ConfigurationManager.AppSettings["MailSMTP"];
            //            smtp.Port = int.Parse(ConfigurationManager.AppSettings["Mailport"]);
            //            smtp.UseDefaultCredentials = false;
            //            smtp.Credentials = new System.Net.NetworkCredential(from, password);
            //            smtp.EnableSsl = true;
            //            try
            //            {
            //                smtp.Send(mail);
            //            }
            //            catch (SmtpException e)
            //            {
            //                return Json(new { Result = "Something error, Please Contact support.", Message = "Error" }, JsonRequestBehavior.AllowGet);
            //            }
            //        }


            //------ End MAIL -----
            return Json(new { Result = "Thank you for contacting us, our Team will get back to you very soon.", Message = "Success" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SaveContactUsDetails()
        {
            PMPMLContactUs contactUsFormDetails = JsonConvert.DeserializeObject<PMPMLContactUs>(Request.Form["contactUsDetails"]);
            PMPMLContactUs obj = new PMPMLContactUs();
            obj.Name = contactUsFormDetails.Name;
            obj.EmailId = contactUsFormDetails.EmailId;
            obj.Subject = contactUsFormDetails.Subject;
            obj.Message = contactUsFormDetails.Message;
            obj.CreatedBy = "";
            obj.CreatedDate = DateTime.Now;
            //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
            UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            db.Insert(obj);

            Crypto crypto = new Crypto(Crypto.CryptoTypes.encTypeTripleDES);
            string from = ConfigurationManager.AppSettings["FromEmail"];
            string password = crypto.Decrypt(ConfigurationManager.AppSettings["FromPassword"]);
            string mailTo = ConfigurationManager.AppSettings["MailReceivers"];
            mailTo = mailTo + "," + contactUsFormDetails.EmailId;
            using (MailMessage mail = new MailMessage(from, mailTo))
            {
                mail.Subject = "PMPML Contact";
                //mail.Body = enquiryDetails.UserName + " having " + enquiryDetails.UserEmail + " has recently contacted " + " for " + mail.Subject + " Please check and revert back.";
                mail.Body = String.Format("<span style='font-family:Verdana; font-size:11px;'>Dear {0},<br/>" +
                    "We have Received your Valuable Feedback.Please find the submitted Feed back Details.<br/>" +
                    "Thanks,<br/>" +
                    "Team PMPML</span><br/><br/><table style='width: 450px'>" +
        "<tr>" +
            "<td colspan='2' style='text-align:center; font-family:Verdana; font-size:11px; font-weight:bold'>Contact Details</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>Name</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{1}</td>" +
        "</tr>" +
            "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px; font-weight:bold'>Email</td>" +
        "</tr>" +
        "<tr>" +
            "<td colspan='2' style='font-family:Verdana; font-size:10px;'>{2}</td>" +
        "</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Subject</td>" +
            "<td style='font-family:Verdana; font-size:10px;'>{3}</td>" +
        "</tr>" +
        "<tr>" +
            "<td style='font-family:Verdana; font-size:10px; font-weight:bold; width: 82px'>Message</td><br>" +
            "<td style='font-family:Verdana; font-size:10px;'>{4}</td>" +
        "</tr>" +


    "</table>", contactUsFormDetails.Name, contactUsFormDetails.Name, contactUsFormDetails.EmailId, contactUsFormDetails.Subject, contactUsFormDetails.Message);

                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["MailSMTP"];
                smtp.Port = int.Parse(ConfigurationManager.AppSettings["Mailport"]);
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(from, password);
                smtp.EnableSsl = false;
                try
                {
                    smtp.Send(mail);
                }
                catch (SmtpException e)
                {
                    return Json(new { Result = "Something error, Please Contact support.", Message = "Error" }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { Result = "", Message = "Success" }, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult BookPuneDarshan()
        {
            StoreConsumerInformation puneDarshanFormDetails = JsonConvert.DeserializeObject<StoreConsumerInformation>(Request.Form["puneDarshanFormDetails"]);
            StoreConsumerInformation obj = new StoreConsumerInformation();
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            // var db1 = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql().Select("*").From("PuneDarshanBusInfo").Where("PaymentType = @0", puneDarshanFormDetails.PaymentType);
            var result = db.SingleOrDefault<AirportServiceInfo>(sql);
            string selectedDate = puneDarshanFormDetails.TravelDate.ToString("yyyy-MM-dd");
            //var sql = new Sql()
            //    .Select("*")
            //    .From("PuneDarshanBusInfo").Where("PaymentType = @0", puneDarshanFormDetails.PaymentType);
            //var result = db.SingleOrDefault<PuneDarshanBusInfo>(sql);
            var sqlForAvailibilitySeats = new Sql()
               .Select("*")
               .From("PMPMLSeatAvailibility").Where("Date = @0", selectedDate);
            var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

            decimal totalAmount = decimal.Parse(result.TotalAmount);
            int selectedSeats = int.Parse(puneDarshanFormDetails.Quantity);
            int totalSeats = TotalAvailibilitySeats.PuneDarshanTotalSeat;
            int availableSeatsStatus = TotalAvailibilitySeats.PuneDarshanSeatAvailibility;
            if (selectedSeats <= availableSeatsStatus)
            {
                obj.BoardingPoint = puneDarshanFormDetails.BoardingPoint;
                obj.TotalAmount = Math.Round((totalAmount * selectedSeats), 2);
                obj.TravelDate = puneDarshanFormDetails.TravelDate;
                obj.TravelTime = puneDarshanFormDetails.TravelTime;
                obj.Email = puneDarshanFormDetails.Email;
                obj.Contact = puneDarshanFormDetails.Contact;
                obj.ProofNumber = puneDarshanFormDetails.ProofNumber;
                obj.IdProof = puneDarshanFormDetails.IdProof;
                obj.PaymentType = puneDarshanFormDetails.PaymentType;
                obj.Quantity = puneDarshanFormDetails.Quantity;
                obj.TermAndCon = puneDarshanFormDetails.TermAndCon;
                obj.ModuleName = PaymentForModules.PuneDarshan.ToString();
                obj.TransactionID = long.Parse(DateTime.Now.ToString("ddMMyyhhmmssff"));
                obj.Route = "";
                obj.Destination = "";
                obj.UniqueReference = "PDN" + "-" + obj.Email.ToString().Substring(0, 3).ToUpper() + "-" + DateTime.Now.ToString("ddMMyyhhmmssff");
                obj.CreatedBy = "";
                obj.CreatedDate = DateTime.Now;
                //obj.CreatedDate = new DateTime();
                //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
                //UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                db.Insert(obj);
                var pmpmlPgConfig = new Sql().Select("*").From("PaymentGatwayConfig").Where("Module = @0", obj.ModuleName); ;
                PaymentGatwayConfig pgConfig = db.SingleOrDefault<PaymentGatwayConfig>(pmpmlPgConfig);
                if (pgConfig == null)
                {
                    //SuccessfullyStored = false;
                    _logger.Error("Payment Gateway has not been enabled for " + obj.ModuleName);
                    //throw new Exception("Payment Gateway has not been enabled for the selected hospital.");
                    return Json(new { Result = "Payment Gateway has not been enabled for " + obj.ModuleName, Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string paymentGatewayURL = PreparePayUForm(pgConfig, obj.Email, obj.Contact, obj.ModuleName, obj.TotalAmount, obj.UniqueReference, obj.TransactionID);
                    return Json(new { Result = paymentGatewayURL, Message = "Success" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { Result = "You are just late. Only" + availableSeatsStatus + "No. of seat(s) are available.", Message = "ERROR" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult BookAirportService()
        {
            StoreConsumerInformation airportServicFormDetails = JsonConvert.DeserializeObject<StoreConsumerInformation>(Request.Form["airportServicFormDetails"]);
            StoreConsumerInformation obj = new StoreConsumerInformation();
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("AirportServiceInfo").Where("PaymentType = @0", airportServicFormDetails.PaymentType);
            string selectedDate = airportServicFormDetails.TravelDate.ToString("yyyy-MM-dd");
            var sqlForAvailibilitySeats = new Sql()
               .Select("*")
               .From("PMPMLSeatAvailibility").Where("Date = @0", selectedDate);
            var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

            var airportRoute = new Sql()
                    .Select("*")
                    .From("PMPMLAirportRouteTable");
            var getRouteDetails = db.Fetch<PMPMLAirportRouteTable>(airportRoute);
            getRouteDetails = getRouteDetails.Where(p => p.Route == airportServicFormDetails.Route).ToList();
            int getRouteDetailsId = getRouteDetails.FirstOrDefault().Id;


            var airportFare = new Sql()
                    .Select("*")
                    .From("PMPMLAirportFares");
            var getAirportFaresDetails = db.Fetch<PMPMLAirportFares>(airportFare);
            getAirportFaresDetails = getAirportFaresDetails.Where(p => p.AirportRouteId == getRouteDetailsId && p.From == airportServicFormDetails.Destination).ToList();
            //  int getRouteDetailsId = getAirportFaresDetails.FirstOrDefault().Fare;
            var result = db.SingleOrDefault<AirportServiceInfo>(sql);
            decimal totalAmount = getAirportFaresDetails.FirstOrDefault().Fare;//decimal.Parse(result.TotalAmount);
            int selectedSeats = int.Parse(airportServicFormDetails.Quantity);
            int totalSeats = TotalAvailibilitySeats.AirportServiceTotalSeat;
            int availableSeatsStatus = TotalAvailibilitySeats.AirportServiceSeatAvailibility;
            if (selectedSeats <= availableSeatsStatus)
            {
                obj.BoardingPoint = airportServicFormDetails.BoardingPoint;
                obj.TotalAmount = Math.Round((totalAmount * selectedSeats), 2);
                obj.TravelDate = airportServicFormDetails.TravelDate;
                obj.TravelTime = airportServicFormDetails.TravelTime;
                obj.Email = airportServicFormDetails.Email;
                obj.Contact = airportServicFormDetails.Contact;
                obj.ProofNumber = airportServicFormDetails.ProofNumber;
                obj.IdProof = airportServicFormDetails.IdProof;
                obj.PaymentType = airportServicFormDetails.PaymentType;
                obj.Quantity = airportServicFormDetails.Quantity;
                obj.TermAndCon = airportServicFormDetails.TermAndCon;
                obj.Route = airportServicFormDetails.Route;
                obj.Destination = airportServicFormDetails.Destination;
                obj.ModuleName = PaymentForModules.AirportService.ToString();
                obj.TransactionID = long.Parse(DateTime.Now.ToString("ddMMyyhhmmssff"));
                obj.UniqueReference = "APS" + "-" + obj.Email.ToString().Substring(0, 3).ToUpper() + "-" + DateTime.Now.ToString("ddMMyyhhmmssff");
                obj.CreatedBy = "";
                obj.CreatedDate = DateTime.Now;


                //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
                //UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                db.Insert(obj);
                var pmpmlPgConfig = new Sql().Select("*").From("PaymentGatwayConfig").Where("Module = @0", obj.ModuleName);
                PaymentGatwayConfig pgConfig = db.SingleOrDefault<PaymentGatwayConfig>(pmpmlPgConfig);
                if (pgConfig == null)
                {
                    //SuccessfullyStored = false;
                    _logger.Error("Payment Gateway has not been enabled for " + obj.ModuleName);
                    //throw new Exception("Payment Gateway has not been enabled for the selected hospital.");
                    return Json(new { Result = "Payment Gateway has not been enabled for " + obj.ModuleName, Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string paymentGatewayURL = PreparePayUForm(pgConfig, obj.Email, obj.Contact, obj.ModuleName, obj.TotalAmount, obj.UniqueReference, obj.TransactionID);
                    return Json(new { Result = paymentGatewayURL, Message = "Success" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { Result = "You are just late. Only" + availableSeatsStatus + "No. of seat(s) are available.", Message = "ERROR" }, JsonRequestBehavior.AllowGet);
            }
        }

        public string PreparePayUForm(PaymentGatwayConfig pgConfig, string email, string contact, string module, decimal totalAmtToCharge, string uniqueRef, long transactionId)
        {
            string[] hashVarsSeq;
            string hash_string = string.Empty;
            if (string.IsNullOrEmpty(hash_string)) // generating hash value
            {
                if (
                    string.IsNullOrEmpty(pgConfig.PayUKey) ||
                    string.IsNullOrEmpty(uniqueRef) ||
                    string.IsNullOrEmpty(module) ||
                    string.IsNullOrEmpty("PayU") ||
                    string.IsNullOrEmpty(pgConfig.PayUKey) ||
                    string.IsNullOrEmpty(pgConfig.PayUSalt) ||
                    string.IsNullOrEmpty(pgConfig.PayUSuccessURL) ||
                    string.IsNullOrEmpty(pgConfig.PayUFailureURL) ||
                    string.IsNullOrEmpty(pgConfig.PayUFailureURL) ||
                    string.IsNullOrEmpty(pgConfig.PayUPaymentURL)
                    )
                {
                    return "";
                }

                else
                {
                    hashVarsSeq = pgConfig.HashSequence.Split('|'); // spliting hash sequence from config
                    hash_string = "";
                    foreach (string hash_var in hashVarsSeq)
                    {
                        if (hash_var == "key")
                        {
                            hash_string = hash_string + pgConfig.PayUKey;
                            hash_string = hash_string + '|';
                        }
                        else if (hash_var == "txnid")
                        {
                            hash_string = hash_string + uniqueRef;
                            hash_string = hash_string + '|';
                        }
                        else if (hash_var == "amount")
                        {
                            hash_string = hash_string + Convert.ToDecimal(totalAmtToCharge).ToString("g29");
                            hash_string = hash_string + '|';
                        }
                        else if (hash_var == "productinfo")
                        {
                            hash_string = hash_string + pgConfig.vpc_OrderInfo;
                            hash_string = hash_string + '|';
                        }
                        else if (hash_var == "firstname")
                        {
                            hash_string = hash_string + "";
                            hash_string = hash_string + '|';
                        }
                        else if (hash_var == "email")
                        {
                            hash_string = hash_string + email;
                            hash_string = hash_string + '|';
                        }
                        else
                        {

                            hash_string = hash_string + "";// isset if else
                            hash_string = hash_string + '|';
                        }
                    }

                    hash_string += pgConfig.PayUSalt; // appending SALT

                    hash1 = GenerateMD5SignatureForPayU(hash_string).ToLower();// Generatehash512(hash_string).ToLower();         //generating hash
                    action1 = pgConfig.PayUPaymentURL + "/_payment";// setting URL

                }


            }

            else if (!string.IsNullOrEmpty(hash_string))
            {
                hash1 = hash_string;
                action1 = pgConfig.PayUPaymentURL + "/_payment";

            }




            if (!string.IsNullOrEmpty(hash1))
            {
                //hash.Value = hash1;
                //txnid.Value = txnid1;

                System.Collections.Hashtable data = new System.Collections.Hashtable(); // adding values in gash table for data post
                data.Add("hash", hash1);
                data.Add("txnid", uniqueRef);
                data.Add("key", pgConfig.PayUKey);
                string AmountForm = Convert.ToDecimal(totalAmtToCharge).ToString("g29");// eliminating trailing zeros
                //amount.Text = AmountForm;
                data.Add("amount", AmountForm);
                data.Add("firstname", "");
                data.Add("email", email);
                data.Add("phone", contact);
                data.Add("productinfo", pgConfig.vpc_OrderInfo);
                data.Add("surl", pgConfig.PayUSuccessURL);
                data.Add("furl", pgConfig.PayUFailureURL);
                data.Add("lastname", "");
                data.Add("curl", pgConfig.PayUCancelURL);

                string strForm = PreparePOSTForm(action1, data);
                return strForm;
                //Page.Controls.Add(new LiteralControl(strForm));

            }

            else
            {
                //no hash
                return "";
            }
        }
        public string GenerateMD5SignatureForPayU(string Rawdata)
        {
            byte[] message = Encoding.UTF8.GetBytes(Rawdata);

            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] hashValue;
            SHA512Managed hashString = new SHA512Managed();
            string hex = "";
            hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += String.Format("{0:x2}", x);
            }
            return hex;
        }
        private string PreparePOSTForm(string url, System.Collections.Hashtable data)      // post form
        {//Set a name for the form
            string formID = "PostForm";
            //Build the form using the specified data to be posted.
            StringBuilder strForm = new StringBuilder();
            strForm.Append("<form id=\"" + formID + "\" name=\"" +
                           formID + "\" action=\"" + url +
                           "\" method=\"POST\">");

            foreach (System.Collections.DictionaryEntry key in data)
            {

                strForm.Append("<input type=\"hidden\" name=\"" + key.Key +
                               "\" value=\"" + key.Value + "\">");
            }


            strForm.Append("</form>");
            //Build the JavaScript which will do the Posting operation.
            StringBuilder strScript = new StringBuilder();
            strScript.Append("<script language='javascript'>");
            strScript.Append("var v" + formID + " = document." +
                             formID + ";");
            strScript.Append("v" + formID + ".submit();");
            strScript.Append("</script>");
            //Return the form and the script concatenated.
            //(The order is important, Form then JavaScript)
            return strForm.ToString() + strScript.ToString();
        }
        //private SortedDictionary<string, string> SortNameValueCollection(System.Collections.Hashtable nvc)
        //{
        //    SortedDictionary<string, string> sortedDict = new SortedDictionary<string, string>();
        //    foreach (String key in nvc.AllKeys)
        //        sortedDict.Add(key, nvc[key]);
        //    return sortedDict;
        //}

        //public static MT_User GetFacebookUser(string fbId)
        //{
        //    var db = ApplicationContext.Current.DatabaseContext.Database;
        //    var sql = new Sql()
        //        .Select("*")
        //        .From("mt_user")
        //        .Where("FBId = @0", fbId);
        //    var user = db.SingleOrDefault<MT_User>(sql);
        //    return user;
        //}
        [HttpGet]
        public ActionResult RetrieveBusInformation(string paymentType, DateTime date)
        {
            string selectedDate = date.ToString("yyyy-MM-dd");
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("PuneDarshanBusInfo").Where("PaymentType = @0", paymentType != "undefined" ? paymentType : "Debit Card");
            var result = db.SingleOrDefault<AirportServiceInfo>(sql);
            var sqlForAvailibilitySeats = new Sql()
                .Select("*")
                .From("PMPMLSeatAvailibility").Where("Date = @0", selectedDate);
            var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);
            if (TotalAvailibilitySeats != null)
            {
                return Json(new { Result = result, Seats = TotalAvailibilitySeats, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { Result = "No Buses are available for Current Date.", Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult RetrieveAirportServiceInformation(string paymentType, DateTime date)
        {
            string selectedDate = date.ToString("yyyy-MM-dd");
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("AirportServiceInfo").Where("PaymentType = @0", paymentType != "undefined" ? paymentType : "Debit Card");
            var result = db.SingleOrDefault<AirportServiceInfo>(sql);
            var sqlForAvailibilitySeats = new Sql()
                .Select("*")
                .From("PMPMLSeatAvailibility").Where("Date = @0", selectedDate);
            var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);
            if (TotalAvailibilitySeats != null)
            {
                return Json(new { Result = result, Seats = TotalAvailibilitySeats, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { Result = "No Buses are available for Current Date.", Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        public FileResult PrintReceipt(int Id)
        {
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql().Select("*").From("StoreConsumerInformation").Where("Id = @0", Id);
            var consumerInfo = db.SingleOrDefault<StoreConsumerInformation>(sql);
            var Values = new List<KeyValuePair<string, string>>();
            Values.Add(new KeyValuePair<string, string>("txtDate", consumerInfo.TravelDate.ToString("dd-MM-yyyy")));
            Values.Add(new KeyValuePair<string, string>("txtTime", consumerInfo.TravelTime));
            if (consumerInfo.ModuleName != PaymentForModules.HireABus.ToString())
            {
                Values.Add(new KeyValuePair<string, string>("txtEmail", consumerInfo.Email));
                Values.Add(new KeyValuePair<string, string>("txtNoOfSeats", consumerInfo.Quantity));
            }
            //Values.Add(new KeyValuePair<string, string>("txtEmail", consumerInfo.Email));
            Values.Add(new KeyValuePair<string, string>("txtContactNo", consumerInfo.Contact));
            Values.Add(new KeyValuePair<string, string>("txtTransactionId", consumerInfo.UniqueReference.ToString()));
            // Values.Add(new KeyValuePair<string, string>("txtNoOfSeats", consumerInfo.Quantity));
            Values.Add(new KeyValuePair<string, string>("txtTotalAmount", Math.Round((consumerInfo.TotalAmount), 2).ToString()));

            var templateReader = new iTextSharp.text.pdf.PdfReader(System.Web.Hosting.HostingEnvironment.MapPath("~/Document/PMPMLReceipt.pdf"));

            byte[] receipt = null;

            receipt = CreateReceipt(templateReader, Values);
            templateReader.Close();

            // HttpContext.Response.ContentType = "application/pdf";
            HttpContext.Response.Clear();
            //MemoryStream ms = new MemoryStream(receipt);
            //HttpContext.Response.ContentType = "application/pdf";
            //HttpContext.Response.AddHeader("content-disposition", "attachment;filename=PMPMLReceipt.pdf");
            //HttpContext.Response.Buffer = true;
            //ms.WriteTo(Response.OutputStream);
            //HttpContext.Response.End();
            //HttpContext.Response.Write(receipt);
            return File(receipt, System.Net.Mime.MediaTypeNames.Application.Pdf, "PMPMLReceipt.pdf");

            //httpResponse.Content = new ByteArrayContent(buffer);

            //httpResponse.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            //httpResponse.Content.Headers.ContentDisposition.FileName = "Prescription.pdf";
            //httpResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            //File.WriteAllBytes(System.Web.Hosting.HostingEnvironment.MapPath("~/PDF Templates/Prescription.pdf"), buffer); // Requires System.IO
            //httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            //  return httpResponse;
        }
        public ActionResult ProcessOrderAfterPayment()
        {
            StoreConsumerInformation result = new StoreConsumerInformation();
            NameValueCollection nvc = Request.Form;
            string from = string.Empty;
            string password = string.Empty;
            if (Request.Form["txnid"] != null)
            {
                string Vmmp_txn = nvc["mihpayid"].ToString();
                string VpaymentStatus = nvc["status"].ToString();
                string Vmer_txn = Request.Form["txnid"];

                if (VpaymentStatus == "success")
                {
                    var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                    var sql = new Sql().Select("*").From("StoreConsumerInformation").Where("UniqueReference = @0", Vmer_txn);
                    //.Where("FBId = @0", fbId);
                    var obj = new PuneDarshanBusInfo();
                    result = db.SingleOrDefault<StoreConsumerInformation>(sql);
                    result.PaymentStatus = "Successful";
                    result.TransationStatus = true;
                    result.TotalAmount = Math.Round(result.TotalAmount, 2);
                    db.Update(result);
                    if (result.ModuleName == PaymentForModules.PuneDarshan.ToString())
                    {
                        from = ConfigurationManager.AppSettings["FromPuneDarshanEmail"];
                        password = ConfigurationManager.AppSettings["FromPuneDarshanPassword"];
                        //var puneDarshanInfo = new Sql().Select("*").From("PuneDarshanBusInfo");
                        //.Where("FBId = @0", fbId);
                        //var obj1 = new PMPMLSeatAvailibility();
                        var sqlForAvailibilitySeats = new Sql().Select("*").From("PMPMLSeatAvailibility").Where("Date = @0", result.TravelDate);
                        var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

                        //var puneDarshanInfoResult = db.SingleOrDefault<PuneDarshanBusInfo>(puneDarshanInfo);
                        int selectedSeats = int.Parse(result.Quantity);
                        int totalSeats = TotalAvailibilitySeats.PuneDarshanTotalSeat;
                        int availableSeatsStatus = TotalAvailibilitySeats.PuneDarshanSeatAvailibility;
                        TotalAvailibilitySeats.PuneDarshanSeatAvailibility = (availableSeatsStatus - selectedSeats);
                        //obj1.TotalAmount = puneDarshanInfoResult.TotalAmount;
                        //obj1.TotalQuantity = puneDarshanInfoResult.TotalQuantity;
                        //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                        //obj1.Id = puneDarshanInfoResult.Id;
                        db.Update(TotalAvailibilitySeats);
                    }
                    else if (result.ModuleName == PaymentForModules.AirportService.ToString())
                    {
                        from = ConfigurationManager.AppSettings["FromAirportServiceEmail"];
                        password = ConfigurationManager.AppSettings["FromAirportServicePassword"];
                        //var puneDarshanInfo = new Sql().Select("*").From("AirportServiceInfo");
                        //.Where("FBId = @0", fbId);
                        //var obj1 = new AirportServiceInfo();
                        //var airportInfoResult = db.SingleOrDefault<AirportServiceInfo>(puneDarshanInfo);
                        var sqlForAvailibilitySeats = new Sql().Select("*").From("PMPMLSeatAvailibility").Where("Date = @0", result.TravelDate);
                        var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

                        int selectedSeats = int.Parse(result.Quantity);
                        int totalSeats = TotalAvailibilitySeats.AirportServiceTotalSeat;
                        int availableSeatsStatus = TotalAvailibilitySeats.AirportServiceSeatAvailibility;
                        TotalAvailibilitySeats.AirportServiceSeatAvailibility = (availableSeatsStatus - selectedSeats);
                        // obj1.TotalAmount = airportInfoResult.TotalAmount;
                        //obj1.TotalQuantity = airportInfoResult.TotalQuantity;
                        //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                        //obj1.Id = airportInfoResult.Id;
                        db.Update(TotalAvailibilitySeats);
                    }
                    else if (result.ModuleName == PaymentForModules.HireABus.ToString())
                    {
                        var sqlForAvailibilityBus = new Sql().Select("*").From("PMPMLHireABus").Where("Date = @0", result.TravelDate);
                        var TotalAvailibilityBus = db.SingleOrDefault<PMPMLHireABus>(sqlForAvailibilityBus);

                        int selectedBus = int.Parse(result.Quantity);
                        int totalBuses = TotalAvailibilityBus.TotalBusForHire;
                        int availableBusStatus = TotalAvailibilityBus.TotalBusAvailibilityForHire;
                        TotalAvailibilityBus.TotalBusAvailibilityForHire = (availableBusStatus - selectedBus);
                        // obj1.TotalAmount = airportInfoResult.TotalAmount;
                        //obj1.TotalQuantity = airportInfoResult.TotalQuantity;
                        //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                        //obj1.Id = airportInfoResult.Id;
                        db.Update(TotalAvailibilityBus);
                    }
                    Crypto crypto = new Crypto(Crypto.CryptoTypes.encTypeTripleDES);
                    //string from = ConfigurationManager.AppSettings["FromEmail"];
                    // string password = ConfigurationManager.AppSettings["FromPassword"]; //crypto.Decrypt(ConfigurationManager.AppSettings["FromPassword"]);
                    string mailTo = ConfigurationManager.AppSettings["MailReceivers"];
                    if (result.Email != "")
                    {
                        mailTo = mailTo + "," + result.Email;
                    }
                    using (MailMessage mail = new MailMessage(from, mailTo))
                    {
                        mail.Subject = ConfigurationManager.AppSettings["ContactSubject"];
                        //mail.Body = enquiryDetails.UserName + " having " + enquiryDetails.UserEmail + " has recently contacted " + " for " + mail.Subject + " Please check and revert back.";
                        mail.Body = String.Format("<span style='font-family:Verdana; font-size:11px;'>Dear Visitor,<br/>" +
                            "You Booking has been Confirmed</a>. Please find the attached receipt along with the mail for all the details.<br/><br/>" +
                            "Thanks,<br/>" +
                            "Team PMPML</span>", result.Email, "07-02-2019", result.TravelTime, result.Email, result.Contact, result.TransactionID, result.Quantity, result.TotalAmount);

                        var Values = new List<KeyValuePair<string, string>>();

                        Values.Add(new KeyValuePair<string, string>("txtDate", "07-02-2019"));
                        Values.Add(new KeyValuePair<string, string>("txtTime", result.TravelTime));
                        if (result.ModuleName != PaymentForModules.HireABus.ToString())
                        {
                            Values.Add(new KeyValuePair<string, string>("txtEmail", result.Email));
                            Values.Add(new KeyValuePair<string, string>("txtNoOfSeats", result.Quantity));
                        }
                        Values.Add(new KeyValuePair<string, string>("txtContactNo", result.Contact));
                        Values.Add(new KeyValuePair<string, string>("txtTransactionId", result.UniqueReference.ToString()));

                        Values.Add(new KeyValuePair<string, string>("txtTotalAmount", Math.Round((result.TotalAmount), 2).ToString()));
                        var templateReader = new iTextSharp.text.pdf.PdfReader(System.Web.Hosting.HostingEnvironment.MapPath("~/Document/PMPMLReceipt.pdf"));

                        byte[] receipt = null;

                        receipt = CreateReceipt(templateReader, Values);
                        templateReader.Close();

                        mail.BodyEncoding = Encoding.UTF8;
                        // Get pdf binary data and save the data to a memory stream
                        System.IO.MemoryStream ms = new System.IO.MemoryStream(receipt);
                        mail.Attachments.Add(new Attachment(ms, "PMPMLReceipt.pdf", "application/pdf"));

                        mail.IsBodyHtml = true;

                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = ConfigurationManager.AppSettings["MailSMTP"];
                        smtp.Port = int.Parse(ConfigurationManager.AppSettings["Mailport"]);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new System.Net.NetworkCredential(from, password);
                        smtp.EnableSsl = false;
                        try
                        {
                            smtp.Send(mail);
                        }
                        catch (SmtpException e)
                        {
                            return Json(new { Result = e.Message.ToString(), Message = "Error" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    return View("../PmpmlResponse/PmpmlPgResponse", result);
                }
                else
                {
                    result.PaymentStatus = "Failed";
                    result.TransationStatus = false;
                    return View("../PmpmlResponse/PmpmlPgResponse", result);
                }
            }
            else
            {
                result.PaymentStatus = "Failed";
                result.TransationStatus = false;
                return View("../PmpmlResponse/PmpmlPgResponse", result);
            }
        }
        private byte[] CreateReceipt(PdfReader templateReader, List<KeyValuePair<string, string>> Values)
        {
            byte[] pageBytes = null;
            try
            {
                using (var tempStream = new System.IO.MemoryStream())
                {
                    PdfStamper stamper = new PdfStamper(templateReader, tempStream);

                    stamper.FormFlattening = true;

                    AcroFields fields = stamper.AcroFields;

                    fields.GenerateAppearances = true;

                    stamper.Writer.CloseStream = false;


                    // Walk the Dictionary keys, fnid teh matching AcroField, 
                    // and set the value:
                    foreach (var element in Values)
                    {
                        fields.SetField(element.Key, element.Value);
                    }

                    // If we had not set the CloseStream property to false, 
                    // this line would also kill our memory stream:
                    stamper.Close();

                    // Reset the stream position to the beginning before reading:
                    tempStream.Position = 0;

                    // Grab the byte array from the temp stream . . .
                    pageBytes = tempStream.ToArray();

                    tempStream.Flush();
                    stamper.Close();
                }
                return pageBytes;
            }
            catch (Exception ex)
            {
                //EmailSender.logger.Error(ex);
                pageBytes = null;
            }
            return pageBytes;
        }
        [CustomSessionAtribute]
        [HttpGet]
        public ActionResult GetNoOfTicketsBookedAndAmountInfo()
        {
            try
            {
                DateTime currentDate = DateTime.Now.Date; //DateTime.Now;
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                var sql = new Sql()
                    .Select("*")
                    .From("storeconsumerinformation");


                var lookupResult = db.Fetch<PMPMLConsumerInfo>(sql);
                List<PMPMLDashboardLookup> dashboardLookupInfo = lookupResult.GroupBy(t => t.ModuleName).Select(s => new PMPMLDashboardLookup
                {
                    ModuleName = s.Key,
                    BoardingPoints = s.Select(t => t.BoardingPoint.Trim()).Distinct().ToList<string>()

                }).ToList();

                var result = db.Fetch<PMPMLConsumerInfo>(sql);
                List<PMPMLBookedTicketAndAmountDashboardModel> bookedTicketAndAmount = result.Where(t => t.CreatedDate.Date == currentDate.Date && t.TransationStatus == true).GroupBy(g => new { g.ModuleName, g.BoardingPoint }).
                    Select(s => new PMPMLBookedTicketAndAmountDashboardModel
                    {
                        ModuleName = s.Key.ModuleName,
                        BoardingPoint = s.Key.BoardingPoint,
                        NoOfTickets = s.Sum(t => t.Quantity),
                        AmountInRupees = s.Sum(t => t.TotalAmount)

                    }).ToList();

                return Json(new { Result = bookedTicketAndAmount, dashboardLookup = dashboardLookupInfo, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [CustomSessionAtribute]
        [HttpGet]
        public ActionResult GetNoOfOccupancyByModuleNameAndBoardPointInfo()
        {
            try
            {
                DateTime currentDate = DateTime.Now.Date;
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                var sqlSeatAvaliability = new Sql()
                    .Select("*")
                    .From("pmpmlseatavailibility");

                var sqlConsumerInfo = new Sql()
                    .Select("*")
                    .From("storeconsumerinformation");

                var resultSeatAvaliability = db.Fetch<PMPMLSeatAvailibility>(sqlSeatAvaliability);
                PMPMLSeatAvailibility seatAvaliabilies = resultSeatAvaliability.Where(t => t.Date.Date == currentDate.Date).FirstOrDefault();

                var resultConsumerInfo = db.Fetch<PMPMLConsumerInfo>(sqlConsumerInfo);
                List<PMPMLBookedTicketAndAmountDashboardModel> NoOfOccupancyByModuleNameAndBoardingPoint = resultConsumerInfo.Where(t => t.TravelDate.Date == currentDate.Date && t.TransationStatus == true).GroupBy(g => new { g.ModuleName, g.BoardingPoint })
                    .Select(s => new PMPMLBookedTicketAndAmountDashboardModel
                    {
                        ModuleName = s.Key.ModuleName,
                        BoardingPoint = s.Key.BoardingPoint,
                        NoOfOccupancy = s.Sum(t => t.Quantity)
                    }).ToList();

                return Json(new { seatAvaliability = seatAvaliabilies, noOfOccupancies = NoOfOccupancyByModuleNameAndBoardingPoint, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }

        }

        private ListOfDimensionWiseRecord GetDimensionWiseTickets(ConsumerQueryString consumerQuery)
        {
            try
            {
                List<string> MonthNames = new List<string>()
                {
                    "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
                };
                DimesionWiseTickets dailyWiseTicketsInfo = new DimesionWiseTickets();
                DateTime currentDate = DateTime.Now.Date; //DateTime.Now.Date;
                DateTime last7Days = currentDate.AddDays(-6);

                List<DateTime> listOfStartDateOftheWeek = new List<DateTime>();

                DateTime startDateOfWeek = DateTime.Now.Date;
                DateTime endDateOfWeek = DateTime.Now.Date;
                int currentDay = Convert.ToInt16(endDateOfWeek.DayOfWeek);

                ListOfDimensionWiseRecord listOfDimensionWiseRecordInfo = new ListOfDimensionWiseRecord();
                listOfDimensionWiseRecordInfo.listOfDailyWiseTickets = new List<DimesionWiseTickets>();
                listOfDimensionWiseRecordInfo.listOfWeeklyWiseTickets = new List<DimesionWiseTickets>();
                listOfDimensionWiseRecordInfo.listOfMonthlyWiseTickets = new List<DimesionWiseTickets>();


                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;

                var sqlConsumerInfo = new Sql()
                    .Select("*")
                    .From("storeconsumerinformation");
                var resultConsumerInfo = db.Fetch<PMPMLConsumerInfo>(sqlConsumerInfo);

                if (consumerQuery.dateType.Trim().Equals("Booking"))
                {
                    List<PMPMLConsumerInfo> yearlyConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Year == currentDate.Year && t.TransationStatus == true).ToList();
                    var dailyWiseRecord = yearlyConsumerInfo.Where(t => t.CreatedDate.Date >= last7Days.Date && t.CreatedDate.Date <= currentDate.Date && t.TransationStatus == true).ToList();

                    //Last 7 Days



                    while (last7Days <= currentDate)
                    {
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        objContext.dimension = last7Days.DayOfWeek.ToString().Substring(0, 3).ToUpper();

                        var dailyRecord = dailyWiseRecord.Where(t => t.CreatedDate.Date == last7Days.Date).ToList();
                        if (dailyRecord.Count() > 0)
                        {
                            objContext.noOfTickets = dailyRecord.Sum(t => t.Quantity);
                            objContext.revenue = dailyRecord.Sum(t => t.TotalAmount);
                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }
                        listOfDimensionWiseRecordInfo.listOfDailyWiseTickets.Add(objContext);
                        last7Days = last7Days.AddDays(1);
                    }

                    //Last 30 Days
                    for (int i = 0; i < 4; i++)
                    {
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        List<PMPMLConsumerInfo> weeklyInfo = new List<PMPMLConsumerInfo>();
                        if (currentDay != 0)
                        {
                            objContext.dimension = "Current Week";
                            startDateOfWeek = currentDate.AddDays(-(currentDay));
                            weeklyInfo = yearlyConsumerInfo.Where(t => t.CreatedDate.Date >= startDateOfWeek.Date && t.CreatedDate.Date <= endDateOfWeek.Date && t.TransationStatus == true).ToList();
                            currentDay = 0;
                        }
                        else
                        {
                            objContext.dimension = "Week" + (i - 4);
                            endDateOfWeek = startDateOfWeek.AddDays(-1);
                            startDateOfWeek = startDateOfWeek.AddDays(-7);
                            weeklyInfo = yearlyConsumerInfo.Where(t => t.CreatedDate.Date >= startDateOfWeek.Date && t.CreatedDate.Date <= endDateOfWeek.Date && t.TransationStatus == true).ToList();
                        }
                        if (weeklyInfo.Count > 0)
                        {
                            objContext.noOfTickets = weeklyInfo.Select(t => t.Quantity).Sum();
                            objContext.revenue = weeklyInfo.Sum(t => t.TotalAmount);
                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }

                        listOfDimensionWiseRecordInfo.listOfWeeklyWiseTickets.Add(objContext);
                    }

                    //Last 12 Month
                    for (int i = 0; i < MonthNames.Count(); i++)
                    {
                        int monthIndex = i + 1;
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        objContext.dimension = MonthNames[i];
                        List<PMPMLConsumerInfo> monthlyInfo = yearlyConsumerInfo.Where(t => t.CreatedDate.Month == monthIndex && t.TransationStatus == true).ToList();
                        if (monthlyInfo.Count() > 0)
                        {
                            objContext.noOfTickets = monthlyInfo.Select(t => t.Quantity).Sum();
                            objContext.revenue = monthlyInfo.Sum(t => t.TotalAmount);
                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }
                        listOfDimensionWiseRecordInfo.listOfMonthlyWiseTickets.Add(objContext);
                    }

                }
                else
                {
                    List<PMPMLConsumerInfo> yearlyConsumerInfo = resultConsumerInfo.Where(t => t.TravelDate.Year == currentDate.Year && t.TransationStatus == true).ToList();

                    var dailyWiseRecord = yearlyConsumerInfo.Where(t => t.TravelDate.Date >= last7Days.Date && t.TravelDate.Date < currentDate.Date && t.TransationStatus == true).ToList();

                    //Last 7 Days

                    while (last7Days <= currentDate)
                    {
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        objContext.dimension = last7Days.DayOfWeek.ToString().Substring(0, 3).ToUpper();
                        var dailyRecord = dailyWiseRecord.Where(t => t.TravelDate.Date == last7Days.Date).ToList();
                        if (dailyRecord.Count() > 0)
                        {
                            objContext.noOfTickets = dailyRecord.Sum(t => t.Quantity);
                            objContext.revenue = dailyRecord.Sum(t => t.TotalAmount);
                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }
                        listOfDimensionWiseRecordInfo.listOfDailyWiseTickets.Add(objContext);
                        last7Days = last7Days.AddDays(1);
                    }
                    //Last 30 Days
                    for (int i = 0; i < 4; i++)
                    {
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        List<PMPMLConsumerInfo> weeklyInfo = new List<PMPMLConsumerInfo>();
                        if (currentDay != 0)
                        {
                            objContext.dimension = "Current Week";
                            startDateOfWeek = currentDate.AddDays(-currentDay);
                            weeklyInfo = yearlyConsumerInfo.Where(t => t.TravelDate.Date >= startDateOfWeek.Date && t.TravelDate.Date <= endDateOfWeek.Date && t.TransationStatus == true).ToList();
                            currentDay = 0;
                        }
                        else
                        {
                            objContext.dimension = "Week" + (i - 4);
                            endDateOfWeek = startDateOfWeek.AddDays(-1);
                            startDateOfWeek = startDateOfWeek.AddDays(-7);
                            weeklyInfo = yearlyConsumerInfo.Where(t => t.TravelDate.Date >= startDateOfWeek.Date && t.TravelDate.Date <= endDateOfWeek.Date && t.TransationStatus == true).ToList();

                        }

                        if (weeklyInfo.Count > 0)
                        {
                            objContext.noOfTickets = weeklyInfo.Select(t => t.Quantity).Sum();
                            objContext.revenue = weeklyInfo.Sum(t => t.TotalAmount);

                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }

                        listOfDimensionWiseRecordInfo.listOfWeeklyWiseTickets.Add(objContext);
                    }

                    //Last 12 Month
                    for (int i = 0; i < MonthNames.Count(); i++)
                    {
                        int monthIndex = i + 1;
                        DimesionWiseTickets objContext = new DimesionWiseTickets();
                        objContext.dimension = MonthNames[i];
                        List<PMPMLConsumerInfo> monthlyInfo = yearlyConsumerInfo.Where(t => t.TravelDate.Month == monthIndex && t.TransationStatus == true).ToList();
                        if (monthlyInfo.Count() > 0)
                        {
                            objContext.noOfTickets = monthlyInfo.Select(t => t.Quantity).Sum();
                            objContext.revenue = monthlyInfo.Sum(t => t.TotalAmount);
                        }
                        else
                        {
                            objContext.noOfTickets = 0;
                            objContext.revenue = 0;
                        }
                        listOfDimensionWiseRecordInfo.listOfMonthlyWiseTickets.Add(objContext);
                    }
                }

                return listOfDimensionWiseRecordInfo;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [CustomSessionAtribute]
        [HttpGet]
        public ActionResult GetCustomerInfoByBoardingPoint(ConsumerQueryString consumerQuery)
        {
            try
            {
                DateTime currentDate = DateTime.Now.Date; //DateTime.Now.Date;
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;

                var sqlConsumerInfo = new Sql()
                    .Select("*")
                    .From("storeconsumerinformation");
                var resultConsumerInfo = db.Fetch<PMPMLConsumerInfo>(sqlConsumerInfo);
                List<PMPMLConsumerInfo> listOfConsumerInfo = new List<PMPMLConsumerInfo>();
                switch (consumerQuery.type)
                {
                    case "Tickets":
                        listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date == currentDate.Date && t.BoardingPoint.Trim().ToUpper() == consumerQuery.boardingPoint.Trim().ToUpper() && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                        break;
                    case "Occupancy":
                        listOfConsumerInfo = resultConsumerInfo.Where(t => t.TravelDate.Date == currentDate.Date && t.BoardingPoint.Trim().ToUpper() == consumerQuery.boardingPoint.Trim().ToUpper() && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                        break;
                    case "Date":
                        if (consumerQuery.dateType.Trim().Equals("Booking"))
                        {
                            listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date >= consumerQuery.startDate.Date && t.CreatedDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                        }
                        else if (consumerQuery.dateType.Trim().Equals("Travel"))
                        {
                            listOfConsumerInfo = resultConsumerInfo.Where(t => t.TravelDate.Date >= consumerQuery.startDate.Date && t.TravelDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.TravelDate).ToList();
                        }
                        else
                        {
                            listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date >= consumerQuery.startDate.Date && t.CreatedDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                        }

                        break;
                    default:
                        break;

                }

                listOfConsumerInfo.ForEach(s =>
                {
                    s.ProofNumber = !String.IsNullOrEmpty(s.ProofNumber.Trim()) ? HideNumber(s.ProofNumber.Trim()) : "";
                });

                //, DimensionRecord = GetDimensionWiseTickets(consumerQuery)

                return Json(new { Result = listOfConsumerInfo, DimensionRecord = GetDimensionWiseTickets(consumerQuery), Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }

        }
        public string HideNumber(string number)
        {
            string hiddenString = number.Substring(number.Length - 2).PadLeft(number.Length, '*');
            return hiddenString;
        }
        [CustomSessionAtribute]
        [HttpGet]
        [AllowAnonymous]
        public void GetConsumerExportedInformation(HttpRequestMessage request, ConsumerQueryString consumerQuery)
        {
            //Do server side form validation for each fields.


            DateTime currentDate = DateTime.Now.Date;
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;

            var sqlConsumerInfo = new Sql()
                .Select("*")
                .From("storeconsumerinformation");
            var resultConsumerInfo = db.Fetch<PMPMLConsumerInfo>(sqlConsumerInfo);
            List<PMPMLConsumerInfo> listOfConsumerInfo = new List<PMPMLConsumerInfo>();
            switch (consumerQuery.type)
            {
                case "Tickets":
                    listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date == currentDate.Date && t.BoardingPoint.Trim().ToUpper() == consumerQuery.boardingPoint.Trim().ToUpper() && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                    break;
                case "Occupancy":
                    listOfConsumerInfo = resultConsumerInfo.Where(t => t.TravelDate.Date == currentDate.Date && t.BoardingPoint.Trim().ToUpper() == consumerQuery.boardingPoint.Trim().ToUpper() && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                    break;
                case "Date":
                    if (consumerQuery.dateType.Trim().Equals("Booking"))
                    {
                        listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date >= consumerQuery.startDate.Date && t.CreatedDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                    }
                    else if (consumerQuery.dateType.Trim().Equals("Travel"))
                    {
                        listOfConsumerInfo = resultConsumerInfo.Where(t => t.TravelDate.Date >= consumerQuery.startDate.Date && t.TravelDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.TravelDate).ToList();
                    }
                    else
                    {
                        listOfConsumerInfo = resultConsumerInfo.Where(t => t.CreatedDate.Date >= consumerQuery.startDate.Date && t.CreatedDate.Date <= consumerQuery.endDate.Date && t.TransationStatus == true).OrderBy(t => t.CreatedDate).ToList();
                    }
                    break;
                default:
                    break;

            }
            listOfConsumerInfo.ForEach(s =>
            {
                s.ProofNumber = !String.IsNullOrEmpty(s.ProofNumber.Trim()) ? HideNumber(s.ProofNumber.Trim()) : "";
            });
            var responseDetails = request.CreateResponse();
            using (var excelFile = new ExcelPackage())
            {
                var worksheet = excelFile.Workbook.Worksheets.Add("Sheet1");
                worksheet.Cells[1, 1].Value = "Unique Reference";
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 2].Value = "Consumer";
                worksheet.Cells[1, 2].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 3].Value = "Contact";
                worksheet.Cells[1, 3].Style.Font.Bold = true;
                worksheet.Cells[1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 4].Value = "Service";
                worksheet.Cells[1, 4].Style.Font.Bold = true;
                worksheet.Cells[1, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 5].Value = "Boarding Point";
                worksheet.Cells[1, 5].Style.Font.Bold = true;
                worksheet.Cells[1, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 5].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 6].Value = "Booking Date";
                worksheet.Cells[1, 6].Style.Font.Bold = true;
                worksheet.Cells[1, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 6].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 7].Value = "Travel Date";
                worksheet.Cells[1, 7].Style.Font.Bold = true;
                worksheet.Cells[1, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 7].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 8].Value = "Time";
                worksheet.Cells[1, 8].Style.Font.Bold = true;
                worksheet.Cells[1, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 8].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 9].Value = "ID Proof";
                worksheet.Cells[1, 9].Style.Font.Bold = true;
                worksheet.Cells[1, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 9].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 10].Value = "ID Proof Ref#";
                worksheet.Cells[1, 10].Style.Font.Bold = true;
                worksheet.Cells[1, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 10].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 11].Value = "Payment Mode";
                worksheet.Cells[1, 11].Style.Font.Bold = true;
                worksheet.Cells[1, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 11].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 12].Value = "#Tickets";
                worksheet.Cells[1, 12].Style.Font.Bold = true;
                worksheet.Cells[1, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 12].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);

                worksheet.Cells[1, 13].Value = "Amount";
                worksheet.Cells[1, 13].Style.Font.Bold = true;
                worksheet.Cells[1, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, 13].Style.Fill.BackgroundColor.SetColor(Color.DarkGray);



                for (int index = 0; index < listOfConsumerInfo.Count; index++)
                {
                    worksheet.Cells[index + 2, 1].Value = listOfConsumerInfo[index].UniqueReference;
                    worksheet.Cells[index + 2, 2].Value = listOfConsumerInfo[index].Email;
                    worksheet.Cells[index + 2, 3].Value = listOfConsumerInfo[index].Contact;
                    worksheet.Cells[index + 2, 4].Value = listOfConsumerInfo[index].ModuleName;
                    worksheet.Cells[index + 2, 5].Value = listOfConsumerInfo[index].BoardingPoint;
                    worksheet.Cells[index + 2, 6].Value = listOfConsumerInfo[index].CreatedDate.Date.ToString("dd-MMM-yyyy");
                    worksheet.Cells[index + 2, 7].Value = listOfConsumerInfo[index].TravelDate.Date.ToString("dd-MMM-yyyy");
                    worksheet.Cells[index + 2, 8].Value = listOfConsumerInfo[index].TravelTime;


                    worksheet.Cells[index + 2, 9].Value = listOfConsumerInfo[index].IdProof;
                    worksheet.Cells[index + 2, 10].Value = listOfConsumerInfo[index].ProofNumber;
                    worksheet.Cells[index + 2, 11].Value = listOfConsumerInfo[index].PaymentType;
                    worksheet.Cells[index + 2, 12].Value = listOfConsumerInfo[index].TotalAmount;


                }
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.Cells.AutoFitColumns();
                System.Web.HttpContext.Current.Response.Clear();
                System.Web.HttpContext.Current.Response.ClearContent();
                System.Web.HttpContext.Current.Response.ClearHeaders();
                System.Web.HttpContext.Current.Response.Buffer = true;
                System.Web.HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
                System.Web.HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                System.Web.HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                System.Web.HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=" + "ConsumerInfo_" + DateTime.Today.ToString("ddMMyyyy") + ".xlsx");
                var ms = new System.IO.MemoryStream();
                excelFile.SaveAs(ms);
                ms.WriteTo(System.Web.HttpContext.Current.Response.OutputStream);
                System.Web.HttpContext.Current.Response.Flush();
                System.Web.HttpContext.Current.Response.End();

            }
        }

        [CustomSessionAtribute]
        [HttpGet]
        public ActionResult LogOut()
        {
            //Clear Session
            Session.Abandon();
            Session.Clear();
            FormsAuthentication.SignOut();
            return Json(new { Result = "Successfully Log Out", Message = "Success" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SubmitLoginForm()
        {
            PMPMLLoginForm userLoginDetails = JsonConvert.DeserializeObject<PMPMLLoginForm>(Request.Form["userLoginDetails"]);
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("PMPMLLoginForm").Where("UserLogin = @0 && Password = @1", userLoginDetails.UserLogin, userLoginDetails.Password);
            //.Where("FBId = @0", fbId);
            var result = db.SingleOrDefault<PMPMLLoginForm>(sql);
            if (result != null)
            {
                Session["UserName"] = userLoginDetails.UserLogin;
                FormsAuthentication.SetAuthCookie(userLoginDetails.UserLogin, true);
                return Json(new { Data = result, Result = "Successfully LoggedIn", Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { Result = "Invalid Email Id or Password", Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult GetNumberOfVisitors()
        {
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("NumberOfVisitors");
            //.Where("FBId = @0", fbId);
            var result = db.SingleOrDefault<NumberOfVisitors>(sql);
            //return user;
            return Json(new { Result = result, Message = "Success" }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult StoreNewVisitor()
        {
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sql = new Sql()
                .Select("*")
                .From("NumberOfVisitors");
            //.Where("FBId = @0", fbId);
            var result = db.SingleOrDefault<NumberOfVisitors>(sql);
            result.NumberOfVisitor += 1;
            db.Update(result);
            return Json(new { Result = result, Message = "Success" }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult PMPMLHireABus()
        {
            PMPMLHireABusForm pmpmlHireABus = JsonConvert.DeserializeObject<PMPMLHireABusForm>(Request.Form["pmpmlHireABus"]);
            //PMPMLHireABus obj = new PMPMLHireABus();
            StoreConsumerInformation obj1 = new StoreConsumerInformation();
            var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
            var sqlForAvailibilityBus = new Sql()
               .Select("*")
               .From("PMPMLHireABus").Where("Date = @0", pmpmlHireABus.FromDate);
            var TotalAvailibilityBus = db.SingleOrDefault<PMPMLHireABus>(sqlForAvailibilityBus);

            // var result = db.SingleOrDefault<AirportServiceInfo>(sql);
            int selectedNumOfBus = 1;
            TotalAvailibilityBus.HireBusTotalAmount = Math.Round((TotalAvailibilityBus.HireBusTotalAmount * selectedNumOfBus), 2);
            int totalBus = TotalAvailibilityBus.TotalBusForHire;
            int availibilityBus = TotalAvailibilityBus.TotalBusAvailibilityForHire;
            if (selectedNumOfBus <= availibilityBus)
            {
                obj1.BoardingPoint = pmpmlHireABus.FromLocation;
                obj1.TotalAmount = Math.Round((TotalAvailibilityBus.HireBusTotalAmount * selectedNumOfBus), 2);
                obj1.TravelDate = pmpmlHireABus.FromDate;
                obj1.TravelTime = pmpmlHireABus.FromTime;
                obj1.Email = "";
                obj1.Contact = pmpmlHireABus.ContactNum;
                obj1.ProofNumber = "";
                obj1.IdProof = "";
                obj1.PaymentType = "";
                obj1.Quantity = selectedNumOfBus.ToString();
                obj1.TermAndCon = pmpmlHireABus.TermAndCon;
                obj1.Route = pmpmlHireABus.FromLocation + "To" + pmpmlHireABus.ToLocation;
                obj1.Destination = pmpmlHireABus.ToLocation;
                obj1.ModuleName = PaymentForModules.HireABus.ToString();
                obj1.TransactionID = long.Parse(DateTime.Now.ToString("ddMMyyhhmmssff"));
                obj1.UniqueReference = "HIR" + "-" + obj1.Contact.ToString().Substring(0, 3).ToUpper() + "-" + DateTime.Now.ToString("ddMMyyhhmmssff");
                obj1.CreatedBy = "";
                obj1.CreatedDate = DateTime.Now;
                //obj.EnquiryDetailsOutput = JsonConvert.SerializeObject(Request.Form["contactUsDetails"]);
                //UmbracoDatabase db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                db.Insert(obj1);
                var pmpmlPgConfig = new Sql().Select("*").From("PaymentGatwayConfig").Where("Module = @0", obj1.ModuleName);
                PaymentGatwayConfig pgConfig = db.SingleOrDefault<PaymentGatwayConfig>(pmpmlPgConfig);
                if (pgConfig == null)
                {
                    //SuccessfullyStored = false;
                    _logger.Error("Payment Gateway has not been enabled for " + obj1.ModuleName);
                    //throw new Exception("Payment Gateway has not been enabled for the selected hospital.");
                    return Json(new { Result = "Payment Gateway has not been enabled for " + obj1.ModuleName, Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string paymentGatewayURL = PreparePayUForm(pgConfig, "", obj1.Contact, obj1.ModuleName, obj1.TotalAmount, obj1.UniqueReference, obj1.TransactionID);
                    return Json(new { Result = paymentGatewayURL, Message = "Success" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { Result = "You are just late. Only" + availibilityBus + "No. of bUS(s) are available.", Message = "ERROR" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetRegularServices(int Page, int Rows)
        {
            //List<RouteStopsInfo> stopsData = dbContext.RouteStopsInfo.OrderByDescending(p=>p.Id).Where(t => t.Up !=null).Skip(Page * Rows).Take(Rows).ToList();
            var regularService = dbContext.RouteStopsInfo.GroupBy(t => t.RouteNo).ToList();
            int totalResultCount = regularService.Count();
            var regularServiceTemp = regularService.Select(s => new RegularService
            {
                RouteNumber = s.Key,
                RouteDescription = s.FirstOrDefault().RouteName,
                StopDetailsUp = s.Where(t => t.Up != null).Select(t => new StopDetails { StopName = t.Up }).ToList(),
                StopDetailsDown = s.Where(t => t.Down != null).Select(t => new StopDetails { StopName = t.Down }).ToList(),
                StopDetailsRing = s.Where(t => t.Ring != null).Select(t => new StopDetails { StopName = t.Ring }).ToList(),
            }).OrderByDescending(p => p.RouteNumber).Skip(Page * Rows).Take(Rows).ToList();
            return Json(new { Result = regularServiceTemp, rows = totalResultCount }, JsonRequestBehavior.AllowGet);
            //  return Json();
        }
        [HttpGet]
        public JsonResult RetrieveAirportServiceRoutes()
        {
            try
            {
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                var sql = new Sql()
                    .Select("*")
                    .From("PMPMLAirportRouteTable");
                //.Where("FBId = @0", fbId);
                var result = db.Fetch<PMPMLAirportRouteTable>(sql).ToList();
                return Json(new { Result = result, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetAllBoardingPointFromDetails(int routeId)
        {
            try
            {
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                var sql = new Sql()
                   .Select("*")
                   .From("PMPMLAirportFares");//.Where("AirportRouteId = @0 ", routeId)

                //.Where("FBId = @0", fbId);
                var result = db.Fetch<PMPMLAirportFares>(sql);
                result = result.Where(p => p.AirportRouteId == routeId).ToList();
                return Json(new { Result = result, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetAllBoardingPointToDetails(int routeId, int boardingId)
        {
            try
            {
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                var sql = new Sql()
                    .Select("*")
                    .From("PMPMLAirportFares");//.Where("Id > @0 && AirportRouteId = @1", boardingId, routeId);
                //.Where("FBId = @0", fbId);
                var result = db.Fetch<PMPMLAirportFares>(sql);
                result = result.Where(p => p.AirportRouteId == routeId && p.Id > boardingId).ToList();
                return Json(new { Result = result, Message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetConsumerInformation(string uniqueReference, string mobile, DateTime date)
        {
            List<StoreConsumerInformation> puneDarshanFormDetails = new List<StoreConsumerInformation>();
            try
            {
                string selectedDate = date.ToString("yyyy-MM-dd");
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                if (!String.IsNullOrEmpty(uniqueReference))
                {
                    puneDarshanFormDetails = db.Fetch<StoreConsumerInformation>(new Sql().Select("*").From("StoreConsumerInformation").Where("UniqueReference = @0", uniqueReference)).ToList();
                }
                else if (!String.IsNullOrEmpty(mobile))
                {
                    puneDarshanFormDetails = db.Fetch<StoreConsumerInformation>(new Sql().Select("*").From("StoreConsumerInformation").Where("Contact = @0 and TravelDate = @1", mobile, selectedDate)).ToList();
                }
                else
                {
                    return Json(new { Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                return Json(new { Result = e.Message.ToString(), Message = "ERROR" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Result = puneDarshanFormDetails, Message = "SUCCESS" }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult SendTemporaryTicket(string uniqueReference, string mobile, DateTime date)
        {
            try
            {
                string selectedDate = date.ToString("yyyy-MM-dd");
                List<StoreConsumerInformation> puneDarshanFormDetails = new List<StoreConsumerInformation>();
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                if (!String.IsNullOrEmpty(uniqueReference))
                {
                    puneDarshanFormDetails = db.Fetch<StoreConsumerInformation>(new Sql().Select("*").From("StoreConsumerInformation").Where("UniqueReference = @0", uniqueReference)).ToList();
                }
                else if (!String.IsNullOrEmpty(mobile))
                {
                    puneDarshanFormDetails = db.Fetch<StoreConsumerInformation>(new Sql().Select("*").From("StoreConsumerInformation").Where("Contact = @0 and TravelDate = @1", mobile, selectedDate)).ToList();
                }
                else
                {
                    return Json(new { Message = "ERROR" }, JsonRequestBehavior.AllowGet);
                }
                foreach (var item in puneDarshanFormDetails)
                {
                    item.TotalAmount = Math.Round(item.TotalAmount, 2);
                    Crypto crypto = new Crypto(Crypto.CryptoTypes.encTypeTripleDES);
                    //string from = ConfigurationManager.AppSettings["FromEmail"];
                    // string password = ConfigurationManager.AppSettings["FromPassword"]; //crypto.Decrypt(ConfigurationManager.AppSettings["FromPassword"]);
                    string mailTo = ConfigurationManager.AppSettings["MailReceivers"];
                    if (item.Email != "")
                    {
                        mailTo = mailTo + "," + item.Email;
                    }
                    string from = String.Empty;
                    string password = String.Empty;
                    if (item.ModuleName == PaymentForModules.PuneDarshan.ToString())
                    {
                        from = ConfigurationManager.AppSettings["FromPuneDarshanEmail"];
                        password = ConfigurationManager.AppSettings["FromPuneDarshanPassword"];
                    }
                    else if (item.ModuleName == PaymentForModules.AirportService.ToString())
                    {
                        from = ConfigurationManager.AppSettings["FromAirportServiceEmail"];
                        password = ConfigurationManager.AppSettings["FromAirportServicePassword"];
                    }
                    using (MailMessage mail = new MailMessage(from, mailTo))
                    {
                        mail.Subject = ConfigurationManager.AppSettings["ContactSubject"];
                        //mail.Body = enquiryDetails.UserName + " having " + enquiryDetails.UserEmail + " has recently contacted " + " for " + mail.Subject + " Please check and revert back.";
                        mail.Body = String.Format("<span style='font-family:Verdana; font-size:11px;'>Dear Visitor,<br/>" +
                            "You Booking has been Confirmed</a>. Please find the attached receipt along with the mail for all the details.<br/><br/>" +
                            "Thanks,<br/>" +
                            "Team PMPML</span>", item.Email, item.TravelDate.ToString("dd-MMM-yyyy"), item.TravelTime, item.Email, item.Contact, item.TransactionID, item.Quantity, item.TotalAmount);

                        var Values = new List<KeyValuePair<string, string>>();

                        Values.Add(new KeyValuePair<string, string>("txtDate", item.TravelDate.ToString("dd-MMM-yyyy")));
                        Values.Add(new KeyValuePair<string, string>("txtTime", item.TravelTime));
                        if (item.ModuleName != PaymentForModules.HireABus.ToString())
                        {
                            Values.Add(new KeyValuePair<string, string>("txtEmail", item.Email));
                            Values.Add(new KeyValuePair<string, string>("txtNoOfSeats", item.Quantity));
                        }
                        Values.Add(new KeyValuePair<string, string>("txtContactNo", item.Contact));
                        Values.Add(new KeyValuePair<string, string>("txtTransactionId", item.UniqueReference.ToString()));

                        Values.Add(new KeyValuePair<string, string>("txtTotalAmount", Math.Round((item.TotalAmount), 2).ToString()));
                        var templateReader = new iTextSharp.text.pdf.PdfReader(System.Web.Hosting.HostingEnvironment.MapPath("~/Document/PMPMLReceipt.pdf"));

                        byte[] receipt = null;

                        receipt = CreateReceipt(templateReader, Values);
                        templateReader.Close();

                        mail.BodyEncoding = Encoding.UTF8;
                        // Get pdf binary data and save the data to a memory stream
                        System.IO.MemoryStream ms = new System.IO.MemoryStream(receipt);
                        mail.Attachments.Add(new Attachment(ms, "PMPMLReceipt.pdf", "application/pdf"));

                        mail.IsBodyHtml = true;

                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = ConfigurationManager.AppSettings["MailSMTP"];
                        smtp.Port = int.Parse(ConfigurationManager.AppSettings["Mailport"]);
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new System.Net.NetworkCredential(from, password);
                        smtp.EnableSsl = false;
                        try
                        {
                            smtp.Send(mail);
                        }
                        catch (SmtpException e)
                        {
                            return Json(new { Result = e.Message.ToString(), Message = "Error" }, JsonRequestBehavior.AllowGet);
                        }
                        bool updateResponse = UpdateConsumerPaymentInformation(item);
                    }
                }
 
            }
            catch (Exception e)
            {
                return Json(new { Result = e.Message.ToString(), Message = "ERROR" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Result = "Success", Message = "SUCCESS" }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public Boolean UpdateConsumerPaymentInformation(StoreConsumerInformation result)
        {
            try
            {
                var db = Umbraco.UmbracoContext.Application.DatabaseContext.Database;
                result.PaymentStatus = "Successful";
                result.TransationStatus = true;
                result.TotalAmount = Math.Round(result.TotalAmount, 2);
                db.Update(result);
                if (result.ModuleName == PaymentForModules.PuneDarshan.ToString())
                {
                    var sqlForAvailibilitySeats = new Sql().Select("*").From("PMPMLSeatAvailibility").Where("Date = @0", result.TravelDate);
                    var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

                    //var puneDarshanInfoResult = db.SingleOrDefault<PuneDarshanBusInfo>(puneDarshanInfo);
                    int selectedSeats = int.Parse(result.Quantity);
                    int totalSeats = TotalAvailibilitySeats.PuneDarshanTotalSeat;
                    int availableSeatsStatus = TotalAvailibilitySeats.PuneDarshanSeatAvailibility;
                    TotalAvailibilitySeats.PuneDarshanSeatAvailibility = (availableSeatsStatus - selectedSeats);
                    //obj1.TotalAmount = puneDarshanInfoResult.TotalAmount;
                    //obj1.TotalQuantity = puneDarshanInfoResult.TotalQuantity;
                    //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                    //obj1.Id = puneDarshanInfoResult.Id;
                    db.Update(TotalAvailibilitySeats);
                }
                else if (result.ModuleName == PaymentForModules.AirportService.ToString())
                {
                    var sqlForAvailibilitySeats = new Sql().Select("*").From("PMPMLSeatAvailibility").Where("Date = @0", result.TravelDate);
                    var TotalAvailibilitySeats = db.SingleOrDefault<PMPMLSeatAvailibility>(sqlForAvailibilitySeats);

                    int selectedSeats = int.Parse(result.Quantity);
                    int totalSeats = TotalAvailibilitySeats.AirportServiceTotalSeat;
                    int availableSeatsStatus = TotalAvailibilitySeats.AirportServiceSeatAvailibility;
                    TotalAvailibilitySeats.AirportServiceSeatAvailibility = (availableSeatsStatus - selectedSeats);
                    // obj1.TotalAmount = airportInfoResult.TotalAmount;
                    //obj1.TotalQuantity = airportInfoResult.TotalQuantity;
                    //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                    //obj1.Id = airportInfoResult.Id;
                    db.Update(TotalAvailibilitySeats);
                }
                else if (result.ModuleName == PaymentForModules.HireABus.ToString())
                {
                    var sqlForAvailibilityBus = new Sql().Select("*").From("PMPMLHireABus").Where("Date = @0", result.TravelDate);
                    var TotalAvailibilityBus = db.SingleOrDefault<PMPMLHireABus>(sqlForAvailibilityBus);

                    int selectedBus = int.Parse(result.Quantity);
                    int totalBuses = TotalAvailibilityBus.TotalBusForHire;
                    int availableBusStatus = TotalAvailibilityBus.TotalBusAvailibilityForHire;
                    TotalAvailibilityBus.TotalBusAvailibilityForHire = (availableBusStatus - selectedBus);
                    // obj1.TotalAmount = airportInfoResult.TotalAmount;
                    //obj1.TotalQuantity = airportInfoResult.TotalQuantity;
                    //obj1.CreatedDate = DateTime.Parse("2017-06-06 09:54:21");
                    //obj1.Id = airportInfoResult.Id;
                    db.Update(TotalAvailibilityBus);
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }

    public class RegularService
    {
        public string RouteNumber { get; set; }
        public string RouteDescription { get; set; }
        public List<StopDetails> StopDetailsUp { get; set; }
        public List<StopDetails> StopDetailsDown { get; set; }
        public List<StopDetails> StopDetailsRing { get; set; }

    }

    public class StopDetails
    {
        public string StopNumber { get; set; }
        public string StopName { get; set; }
    }

    public class Crypto
    {
        #region enums, constants & fields
        //types of symmetric encyption
        public enum CryptoTypes
        {
            encTypeDES = 0,
            encTypeRC2,
            encTypeRijndael,
            encTypeTripleDES
        }

        private static readonly string CRYPT_DEFAULT_PASSWORD = Assembly.GetExecutingAssembly().GetName().GetPublicKey().ToString();
        private const CryptoTypes CRYPT_DEFAULT_METHOD = CryptoTypes.encTypeRijndael;

        private byte[] mKey = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
        private byte[] mIV = { 65, 110, 68, 26, 69, 178, 200, 219 };
        private byte[] SaltByteArray = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        private CryptoTypes mCryptoType = CRYPT_DEFAULT_METHOD;
        private string mPassword = CRYPT_DEFAULT_PASSWORD;
        #endregion

        #region Constructors

        public Crypto()
        {
            calculateNewKeyAndIV();
        }

        public Crypto(CryptoTypes CryptoType)
        {
            this.CryptoType = CryptoType;
        }
        #endregion

        #region Props

        /// <summary>
        ///     type of encryption / decryption used
        /// </summary>
        public CryptoTypes CryptoType
        {
            get
            {
                return mCryptoType;
            }
            set
            {
                if (mCryptoType != value)
                {
                    mCryptoType = value;
                    calculateNewKeyAndIV();
                }
            }
        }

        /// <summary>
        ///     Passsword Key Property.
        ///     The password key used when encrypting / decrypting
        /// </summary>
        public string Password
        {
            get
            {
                return mPassword;
            }
            set
            {
                if (mPassword != value)
                {
                    mPassword = value;
                    calculateNewKeyAndIV();
                }
            }
        }
        #endregion

        #region Encryption

        /// <summary>
        ///     Encrypt a string
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText)
        {
            //declare a new encoder
            UTF8Encoding UTF8Encoder = new UTF8Encoding();
            //get byte representation of string
            byte[] inputBytes = UTF8Encoder.GetBytes(inputText);

            //convert back to a string
            return Convert.ToBase64String(EncryptDecrypt(inputBytes, true));
        }

        /// <summary>
        ///     Encrypt string with user defined password
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="password">password to use when encrypting</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, string password)
        {
            this.Password = password;
            return this.Encrypt(inputText);
        }

        /// <summary>
        ///     Encrypt string acc. to cryptoType and with user defined password
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="password">password to use when encrypting</param>
        /// <param name="cryptoType">type of encryption</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, string password, CryptoTypes cryptoType)
        {
            mCryptoType = cryptoType;
            return this.Encrypt(inputText, password);
        }

        /// <summary>
        ///     Encrypt string acc. to cryptoType
        /// </summary>
        /// <param name="inputText">text to encrypt</param>
        /// <param name="cryptoType">type of encryption</param>
        /// <returns>an encrypted string</returns>
        public string Encrypt(string inputText, CryptoTypes cryptoType)
        {
            this.CryptoType = cryptoType;
            return this.Encrypt(inputText);
        }

        #endregion

        #region Decryption

        /// <summary>
        ///     decrypts a string
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText)
        {
            //declare a new encoder
            UTF8Encoding UTF8Encoder = new UTF8Encoding();
            //get byte representation of string
            byte[] inputBytes = Convert.FromBase64String(inputText);

            //convert back to a string
            return UTF8Encoder.GetString(EncryptDecrypt(inputBytes, false));
        }

        /// <summary>
        ///     decrypts a string using a user defined password key
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="password">password to use when decrypting</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, string password)
        {
            this.Password = password;
            return Decrypt(inputText);
        }

        /// <summary>
        ///     decrypts a string acc. to decryption type and user defined password key
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="password">password key used to decrypt</param>
        /// <param name="cryptoType">type of decryption</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, string password, CryptoTypes cryptoType)
        {
            mCryptoType = cryptoType;
            return Decrypt(inputText, password);
        }

        /// <summary>
        ///     decrypts a string acc. to the decryption type
        /// </summary>
        /// <param name="inputText">string to decrypt</param>
        /// <param name="cryptoType">type of decryption</param>
        /// <returns>a decrypted string</returns>
        public string Decrypt(string inputText, CryptoTypes cryptoType)
        {
            this.CryptoType = cryptoType;
            return Decrypt(inputText);
        }
        #endregion

        #region Symmetric Engine

        /// <summary>
        ///     performs the actual enc/dec.
        /// </summary>
        /// <param name="inputBytes">input byte array</param>
        /// <param name="Encrpyt">wheather or not to perform enc/dec</param>
        /// <returns>byte array output</returns>
        private byte[] EncryptDecrypt(byte[] inputBytes, bool Encrpyt)
        {
            //get the correct transform
            ICryptoTransform transform = getCryptoTransform(Encrpyt);

            //memory stream for output
            MemoryStream memStream = new MemoryStream();

            try
            {
                //setup the cryption - output written to memstream
                CryptoStream cryptStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);

                //write data to cryption engine
                cryptStream.Write(inputBytes, 0, inputBytes.Length);

                //we are finished
                cryptStream.FlushFinalBlock();

                //get result
                byte[] output = memStream.ToArray();

                //finished with engine, so close the stream
                cryptStream.Close();

                return output;
            }
            catch (Exception e)
            {
                //throw an error
                throw new Exception("Error in symmetric engine. Error : " + e.Message, e);
            }
        }

        /// <summary>
        ///     returns the symmetric engine and creates the encyptor/decryptor
        /// </summary>
        /// <param name="encrypt">whether to return a encrpytor or decryptor</param>
        /// <returns>ICryptoTransform</returns>
        private ICryptoTransform getCryptoTransform(bool encrypt)
        {
            SymmetricAlgorithm SA = selectAlgorithm();
            SA.Key = mKey;
            SA.IV = mIV;
            if (encrypt)
            {
                return SA.CreateEncryptor();
            }
            else
            {
                return SA.CreateDecryptor();
            }
        }
        /// <summary>
        ///     returns the specific symmetric algorithm acc. to the cryptotype
        /// </summary>
        /// <returns>SymmetricAlgorithm</returns>
        private SymmetricAlgorithm selectAlgorithm()
        {
            SymmetricAlgorithm SA;
            switch (mCryptoType)
            {
                case CryptoTypes.encTypeDES:
                    SA = DES.Create();
                    break;
                case CryptoTypes.encTypeRC2:
                    SA = RC2.Create();
                    break;
                case CryptoTypes.encTypeRijndael:
                    SA = Rijndael.Create();
                    break;
                case CryptoTypes.encTypeTripleDES:
                    SA = TripleDES.Create();
                    break;
                default:
                    SA = TripleDES.Create();
                    break;
            }
            return SA;
        }

        /// <summary>
        ///     calculates the key and IV acc. to the symmetric method from the password
        ///     key and IV size dependant on symmetric method
        /// </summary>
        private void calculateNewKeyAndIV()
        {
            //use salt so that key cannot be found with dictionary attack
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(mPassword, SaltByteArray);
            SymmetricAlgorithm algo = selectAlgorithm();
            mKey = pdb.GetBytes(algo.KeySize / 8);
            mIV = pdb.GetBytes(algo.BlockSize / 8);
        }
        #endregion
    }

    public class UmbracoCareer
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int PmpmlFeedbackDetailsID { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Name { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime DOB { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string State { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string City { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string EmailId { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime ContactNumber { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string OptContactNumber { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Position { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Experience { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string KeySkills { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Organisation { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class PMPMLFeedBack
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string FirstName { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string LastName { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Email { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string ContactNum { get; set; }
        public string Category { get; set; }
        //public string SubCategory { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Suggestion { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class UmbracoGreivance
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int PmpmlGreivanceID { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Name { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime Date { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Title { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string EmployeeId { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string HomeMailAddress { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string WorkMailAddress { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string AccountDetails { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string GreivancePurpose { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

    }
    //public class StoreConsumerInformation
    //{
    //    [PrimaryKeyColumn(AutoIncrement = true)]
    //    public int Id { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string ModuleName { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string BoardingPoint { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public decimal TotalAmount { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public DateTime TravelDate { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string TravelTime { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string Email { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string Contact { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string ProofNumber { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string IdProof { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string Quantity { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string PaymentType { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public Boolean TermAndCon { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string UniqueReference { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public long TransactionID { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string PaymentStatus { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public Boolean TransationStatus { get; set; }
    //    [NullSetting(NullSetting = NullSettings.Null)]
    //    public string CreatedBy { get; set; }
    //    public DateTime CreatedDate { get; set; }

    //}
    public class PuneDarshanBusInfo
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PaymentType { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class AirportServiceInfo
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PaymentType { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TotalAmount { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime CreatedDate { get; set; }
    }
    public class PaymentGatwayConfig
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int PgConfigId { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Module { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PaymentGateWayType { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUCancelURL { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUFailureURL { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUKey { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string vpc_OrderInfo { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string HashSequence { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUPaymentURL { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUSalt { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PayUSuccessURL { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime UpdatedDate { get; set; }
    }
    public enum PaymentForModules
    {
        [Description("PuneDarshan")]
        PuneDarshan,
        [Description("AirportService")]
        AirportService,
        [Description("HireABus")]
        HireABus,
        [Description("SchoolBus")]
        SchoolBus,
    }
    public class StoreConsumerInformation
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string ModuleName { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string BoardingPoint { get; set; }
        public string Route { get; set; }
        public string Destination { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal TotalAmount { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime TravelDate { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TravelTime { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Email { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Contact { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string ProofNumber { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string IdProof { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Quantity { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PaymentType { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public Boolean TermAndCon { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string UniqueReference { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public long TransactionID { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PaymentStatus { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public Boolean TransationStatus { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class PMPMLLoginForm
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string UserName { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string UserLogin { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Password { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public Boolean IsRemember { get; set; }
    }
    public class NumberOfVisitors
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int NumberOfVisitor { get; set; }

    }
    public class PMPMLSeatAvailibility
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime Date { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int AirportServiceSeatAvailibility { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int AirportServiceTotalSeat { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int PuneDarshanSeatAvailibility { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int PuneDarshanTotalSeat { get; set; }

    }
    public class PMPMLHireABus
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int TotalBusAvailibilityForHire { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int TotalBusForHire { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal HireBusTotalAmount { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int TotalBusAvailibilityForSchool { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int TotalBusForSchool { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal SchoolBusTotalAmount { get; set; }


    }
    public class PMPMLHireABusForm
    {
        public string NameOfRequestor { get; set; }
        public string AddressOfRequestor { get; set; }
        public string ContactNum { get; set; }
        public string OptContactNum { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromLocation { get; set; }
        public string ToLocation { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string Purpose { get; set; }
        public string OtherPurpose { get; set; }
        public Boolean TermAndCon { get; set; }

    }
    public class PMPMLContactUs
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string EmailId { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Subject { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Message { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class PMPMLAirportRouteTable
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Route { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class PMPMLAirportFares
    {
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int AirportRouteId { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string From { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public int Fare { get; set; }
        [NullSetting(NullSetting = NullSettings.Null)]
        public string TimeSlots { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PMPMLConsumerInfo
    {
        public long Id { get; set; }
        public string ModuleName { get; set; }
        public string BoardingPoint { get; set; }
        public string Route { get; set; }
        public string Destination { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TravelDate { get; set; }
        public string TravelTime { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string ProofNumber { get; set; }
        public string IdProof { get; set; }
        public int Quantity { get; set; }
        public string PaymentType { get; set; }
        public bool TermAndCon { get; set; }
        public string UniqueReference { get; set; }
        public long TransactionID { get; set; }
        public string PaymentStatus { get; set; }
        public bool TransationStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

    }

    public class PMPMLSeatAvalibility
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public int AirportServiceSeatAvailibility { get; set; }
        public int AirportServiceTotalSeat { get; set; }
        public int PuneDarshanSeatAvailibility { get; set; }
        public int PuneDarshanTotalSeat { get; set; }
    }

    public class PMPMLBookedTicketAndAmountDashboardModel
    {
        public string ModuleName { get; set; }
        public string BoardingPoint { get; set; }
        public long? NoOfTickets { get; set; }
        public decimal? AmountInRupees { get; set; }
        public long? NoOfOccupancy { get; set; }
    }

    public class PMPMLDashboardLookup
    {
        public string ModuleName { get; set; }
        public List<string> BoardingPoints { get; set; }
    }

    public class ConsumerQueryString
    {
        public string boardingPoint { get; set; }
        public string type { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string dateType { get; set; }
    }

    public class DimesionWiseTickets
    {
        public string dimension { get; set; }
        public int noOfTickets { get; set; }
        public decimal revenue { get; set; }

    }

    public class ListOfDimensionWiseRecord
    {
        public List<DimesionWiseTickets> listOfDailyWiseTickets { get; set; }
        public List<DimesionWiseTickets> listOfWeeklyWiseTickets { get; set; }
        public List<DimesionWiseTickets> listOfMonthlyWiseTickets { get; set; }
    }

}