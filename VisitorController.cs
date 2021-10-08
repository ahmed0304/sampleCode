using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EtihadForex.Areas.Admin.ViewModel;
using EtihadForex.Areas.User.ViewModel;
using EtihadForex.Data.Abstract;
using EtihadForex.Email;
using EtihadForex.Models;
using EtihadForex.SiteSystem.SendGrid;
using EtihadForex.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EtihadForex.Controllers
{
    public class VisitorController : Controller
    {
        //private static string baseurl = "https://localhost:44375/";
        private readonly ILogger<VisitorController> _logger;
        private readonly AppHttpClient _appHttpClient;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly ISectionTypeRepository _sectionTypeRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;
        public VisitorController(ILogger<VisitorController> logger, AppHttpClient client,
            IExchangeRateRepository exchangeRateRepository, ISectionTypeRepository sectionTypeRepository,
            ICurrencyRepository currencyRepository, IPaymentMethodRepository paymentMethodRepository, IEmailSender emailSender, IConfiguration configuration)
        {
            this._logger = logger;
            this._appHttpClient = client;
            this._exchangeRateRepository = exchangeRateRepository;
            this._currencyRepository = currencyRepository;
            this._paymentMethodRepository = paymentMethodRepository;
            this._sectionTypeRepository = sectionTypeRepository;
            this._emailSender = emailSender;
            this._config = configuration;
        }

        public async Task<IActionResult> Index()
        {
            RemittanceIndexViewModel model = new RemittanceIndexViewModel();
            try
            {
                model.Recipients = new List<RecipientViewModel>();
                model.TransferReasons = new List<TransferReasonViewModel>();
                model.FundSources = new List<FundSourceViewModel>();
                model.PaymentSources = new List<PaymentSourceViewModel>();
                model.BankAccounts = new List<BankAccountViewModel>();
                //model.PaymentMethod = await GetPaymentMethods();
                model.PaymentMethod = new List<PaymentMethodViewModel>();
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message);
            }
            var currencies = await _currencyRepository.GetAll();
            ViewBag.SrcCurrencies = currencies.Where(x => x.IsActiveSrcCurrency == true);
            ViewBag.DestCurrencies = currencies.Where(x => x.IsActiveDestCurrency == true);
            ViewBag.PaymentMethod = model.PaymentMethod;
            return View(model);
        }

        public async Task<IActionResult> AboutUs()
        {
            var aboutUsDetail = _sectionTypeRepository.GetAll()?.Result;            
            ViewBag.SectionDescription = aboutUsDetail != null ? aboutUsDetail.Where(x => x.IsActive == true && x.SectionName == "AboutUs")?.FirstOrDefault()?.SectionDescription : "<p>This is About us page</p>";
            
            return View();
        }

        public async Task<IActionResult> PrivacyPolicy()
        {
            var privacyDetail = _sectionTypeRepository.GetAll()?.Result;
            ViewBag.SectionDescription = privacyDetail != null ? privacyDetail.Where(x => x.IsActive == true && x.SectionName.ToLower() == "privacypolicy")?.FirstOrDefault()?.SectionDescription : "<p>This is Privacy Policy page</p>";

            return View();
        }
        public async Task<IActionResult> TermsAndCondition()
        {
            var termsDetail = _sectionTypeRepository.GetAll()?.Result;
            ViewBag.SectionDescription = termsDetail != null ? termsDetail.Where(x => x.IsActive == true && x.SectionName.ToLower() == "termsandcondition")?.FirstOrDefault()?.SectionDescription : "<p>This is Terms and Conditions page</p>";
            
            return View();
        }
        public async Task<IActionResult> ContactUs()
        {
            ContactUsEmailViewModel model = new ContactUsEmailViewModel();
            ViewBag.MessageStatus = "";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ContactUs(ContactUsEmailViewModel model)
        {
            if (ModelState.IsValid)
            {           
                var emailList = new List<string> { "support@etihadforex.com.au" };
                string body = string.Format("This message is sent from {0}, his email is {1}, with this message {2}", model.Name, model.Email, model.Message);
                // var emailResponse = await _emailSender.SendEmailAsync(emailList, model.Subject, body);
                new EmailHelper(_config).SendEmailBySMTP(emailList, model.Subject, body);

                ViewBag.MessageStatus = "Your message sent to support team, we'll get back to you soon.";
                model = new ContactUsEmailViewModel();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid form values.");
            }

            return View(model);
        }

        //[HttpPost]
        //public async Task<IActionResult> ContactUs(ContactUsEmailViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (!ValidarCaptcha(model.g_recaptcha_response))
        //        {
        //            ModelState.AddModelError(string.Empty, "Invalid Captcha Response.");
        //            return View(model);
        //        }

        //        var emailList = new List<string> { "support@etihadforex.com.au" };
        //        string body = string.Format("This message is sent from {0}, his email is {1}, with this message {2}", model.Name, model.Email, model.Message);
        //        var emailResponse = await _emailSender.SendEmailAsync(emailList, model.Subject, body);

        //        ViewBag.MessageStatus = "Your message sent to support team, we'll get back to you soon.";
        //        model = new ContactUsEmailViewModel();
        //    }
        //    else
        //    {
        //        ModelState.AddModelError(string.Empty, "Invalid form values.");
        //    }

        //    return View(model);
        //    //return RedirectToAction("Index");
        //}

        private bool ValidarCaptcha(string captchaResponse)
        {
            Stream dataStream = null;
            WebResponse response = null;
            StreamReader reader = null;
            bool isHuman = false;

            try
            {
                WebRequest request = WebRequest.Create("https://www.google.com/recaptcha/api/siteverify");
                request.Method = "POST";

                //Solicitud
                string strPrivateKey = "6Ldc4doZAAAAAN0AYrjpBnpsW9y55fF4WMB9zEiw";

                NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
                outgoingQueryString.Add("secret", strPrivateKey);
                outgoingQueryString.Add("remoteip", "");
                outgoingQueryString.Add("response", captchaResponse);
                string postData = outgoingQueryString.ToString();

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;

                //Respuesta
                dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(dataStream);
                string result = streamReader.ReadToEnd();
                ReCaptchaResponse reCaptchaResponse = JsonConvert.DeserializeObject<ReCaptchaResponse>(result);
                isHuman = reCaptchaResponse.Success;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                //Clean up the streams.
                if (reader != null)
                    reader.Close();
                if (dataStream != null)
                    dataStream.Close();
                if (response != null)
                    response.Close();
            }

            return isHuman;
        }

        public async Task<IActionResult> FAQs()
        {
            var faqsDetail = _sectionTypeRepository.GetAll()?.Result;
            ViewBag.SectionDescription = faqsDetail != null ? faqsDetail.Where(x => x.IsActive == true && x.SectionName.ToLower() == "faqs")?.FirstOrDefault()?.SectionDescription : "<p>This is FAQs page</p>";
            
            return View();
        }

        public async Task<IActionResult> Blogs()
        {
            var blogsDetail = _sectionTypeRepository.GetAll()?.Result;
            var blogsList = blogsDetail != null ? blogsDetail.Where(x => x.IsActive == true && x.SectionName.ToLower() == "blog") : null;
            List<string> blogsDescription = new List<string>();
            if (blogsList != null && blogsList.Count() > 0)
                foreach (var blog in blogsList)
                    blogsDescription.Add(blog.SectionDescription);

            ViewBag.SectionDescription = blogsDescription;


            return View();
        }
        public async Task<IActionResult> ExchangeRates()
        {

            //IEnumerable<ExchangeRateViewModel> model = new List<ExchangeRateViewModel>();
            //var httpResponse = await _appHttpClient.Client.GetAsync("ExchangeRate/GetAll");

            //if (httpResponse.IsSuccessStatusCode)
            //{
            //    var response = await httpResponse.Content.ReadAsAsync<IEnumerable<ExchangeRateViewModel>>();
            //    model = response.OrderBy(x => x.DestCountry.CountryName);
            //}
            //else
            //{
            //    model = Array.Empty<ExchangeRateViewModel>();
            //}
            //return View(model);
            IList<ExchangeRateViewModel> model = new List<ExchangeRateViewModel>();
            var collection = await _exchangeRateRepository.GetAll();

            if (collection != null && collection.Count() > 0)
                foreach (var item in collection)
                {

                    ExchangeRateViewModel exchangeRateViewModel = new ExchangeRateViewModel();

                    exchangeRateViewModel.ExchangeRateID = item.ExchangeRateID;
                    exchangeRateViewModel.SrcCountryID = item.SrcCountryID;
                    exchangeRateViewModel.SrcCurrencyID = item.SrcCurrencyID;

                    exchangeRateViewModel.DestCountryID = item.DestCountryID;
                    exchangeRateViewModel.DestCurrencyID = item.DestCurrencyID;

                    exchangeRateViewModel.Rate = item.Rate;
                    exchangeRateViewModel.InverseRate = item.InverseRate;

                    if (item.SrcCountry != null)
                        exchangeRateViewModel.SrcCountry = new CountryViewModel
                        {
                            CountryID = item.SrcCountry.CountryID,
                            CountryName = item.SrcCountry.CountryName
                        };

                    if (item.SrcCurreny != null)
                        exchangeRateViewModel.SrcCurrency = new CurrencyViewModel
                        {
                            CurrencyID = item.SrcCurreny.CurrencyID,
                            CurrencyName = item.SrcCurreny.CurrencyName
                        };

                    if (item.DestCountry != null)
                        exchangeRateViewModel.DestCountry = new CountryViewModel
                        {
                            CountryID = item.DestCountry.CountryID,
                            CountryName = item.DestCountry.CountryName,
                        };

                    if (item.DestCurrency != null)
                        exchangeRateViewModel.DestCurrency = new CurrencyViewModel
                        {
                            CurrencyID = item.DestCurrency.CurrencyID,
                            CurrencyName = item.DestCurrency.CurrencyName,
                            CurrencyCode = item.DestCurrency.CurrencyCode
                        };

                    if (item.PaymentMethod != null)
                        exchangeRateViewModel.PaymentMethod = new PaymentMethodViewModel
                        {
                            PaymentMethodID = item.PaymentMethod.PaymentMethodID,
                            Name = item.PaymentMethod.Name
                        };

                    model.Add(exchangeRateViewModel);
                }

            return View(model);
        }

        private async Task<IEnumerable<CurrencyViewModel>> GetAllCurrencies()
        {
            IEnumerable<CurrencyViewModel> currencies = new List<CurrencyViewModel>();
            string token = HttpContext.Session.GetString("token");
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("Currency/GetAll");

            if (httpResponse.IsSuccessStatusCode)
            {
                currencies = await httpResponse.Content.ReadAsAsync<IEnumerable<CurrencyViewModel>>();
            }
            else
            {
                currencies = Array.Empty<CurrencyViewModel>();
            }
            return currencies;
        }
        private async Task<IEnumerable<RecipientViewModel>> GetAllRecipients()
        {
            string token = HttpContext.Session.GetString("token");
            IEnumerable<RecipientViewModel> model = new List<RecipientViewModel>();
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("Recipient/GetAllRecipients");
            if (httpResponse.IsSuccessStatusCode)
            {
                var collection = await httpResponse.Content.ReadAsAsync<IEnumerable<RecipientViewModel>>();
                model = collection;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
            }
            return model;
        }

        private async Task<IEnumerable<TransferReasonViewModel>> GetAllTransferReasons()
        {
            string token = HttpContext.Session.GetString("token");
            IEnumerable<TransferReasonViewModel> model = new List<TransferReasonViewModel>();
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("TransferReason/GetAll");
            if (httpResponse.IsSuccessStatusCode)
            {
                var collection = await httpResponse.Content.ReadAsAsync<IEnumerable<TransferReasonViewModel>>();
                model = collection;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
            }
            return model;
        }

        private async Task<IEnumerable<FundSourceViewModel>> GetAllFundSources()
        {
            string token = HttpContext.Session.GetString("token");
            IEnumerable<FundSourceViewModel> model = new List<FundSourceViewModel>();
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("FundSource/GetAll");
            if (httpResponse.IsSuccessStatusCode)
            {
                var collection = await httpResponse.Content.ReadAsAsync<IEnumerable<FundSourceViewModel>>();
                model = collection;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
            }
            return model;
        }

        private async Task<IEnumerable<PaymentSourceViewModel>> GetAllPaymentSources()
        {
            string token = HttpContext.Session.GetString("token");
            IEnumerable<PaymentSourceViewModel> model = new List<PaymentSourceViewModel>();
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("PaymentSource/GetAll");
            if (httpResponse.IsSuccessStatusCode)
            {
                var collection = await httpResponse.Content.ReadAsAsync<IEnumerable<PaymentSourceViewModel>>();
                model = collection;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
            }
            return model;
        }

        private async Task<IEnumerable<BankAccountViewModel>> GetAllBankAccounts()
        {
            string token = HttpContext.Session.GetString("token");
            IEnumerable<BankAccountViewModel> model = new List<BankAccountViewModel>();
            _appHttpClient.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponse = await _appHttpClient.Client.GetAsync("BankAccount/GetAll");
            if (httpResponse.IsSuccessStatusCode)
            {
                var collection = await httpResponse.Content.ReadAsAsync<IEnumerable<BankAccountViewModel>>();
                model = collection;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
            }
            return model;
        }
    }
}