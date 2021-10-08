using EtihadForex.Areas.Admin.ViewModel;
using EtihadForex.Areas.User.ViewModel;
using EtihadForex.Data.Abstract;
using EtihadForex.Data.Enum;
using EtihadForex.Data.Models;
using EtihadForex.Email;
using EtihadForex.EtihadForexAPIs.Interfaces;
using EtihadForex.Models;
using EtihadForex.SiteSystem.SendGrid;
using EtihadForex.Utilities.Attributes;
using EtihadForex.Utilities.Extensions;
using EtihadForex.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EtihadForex.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IUserPersonalDetailRepository _userPersonalDetailRepository;
        private readonly IUserTransferDetailRepository _userTransferDetailRepository;
        private readonly IUserDocumentDetailRepository _userDocumentDetailRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        bool isSuccess = false;
        bool isWarning = false;
        string message = string.Empty;

        public AccountController(ILogger<AccountController> logger, AppHttpClient client,
            IUserTransferDetailRepository userTransferDetailRepository, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService, IUserPersonalDetailRepository userPersonalDetailRepository,
            IUserDocumentDetailRepository userDocumentDetailRepository, IDocumentRepository documentRepository,
            ICurrencyRepository currencyRepository, ICountryRepository countryRepository, ICompanyRepository companyRepository,
            IEmailSender emailSender, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.logger = logger;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._emailService = emailService;
            this._userTransferDetailRepository = userTransferDetailRepository;
            this._userPersonalDetailRepository = userPersonalDetailRepository;
            this._userDocumentDetailRepository = userDocumentDetailRepository;
            this._documentRepository = documentRepository;
            this._currencyRepository = currencyRepository;
            this._countryRepository = countryRepository;
            this._companyRepository = companyRepository;
            this._emailSender = emailSender;
            this._httpContextAccessor = httpContextAccessor;
            this._config = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {

            var externalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            LoginViewModel model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = externalLogins
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {

            //model.ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null && !user.EmailConfirmed && (await _userManager.CheckPasswordAsync(user, model.Password)))
                {
                    ModelState.AddModelError(string.Empty, "Email not confirmed yet");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        //if (!string.IsNullOrWhiteSpace(user.Id))
                        //    HttpContext.Session.SetString("userId", user.Id);

                        if (!string.IsNullOrWhiteSpace(user.Id))
                        {
                            // Get records from UserPersonalDetail
                            var userPersonalDetailFound = await _userPersonalDetailRepository.GetUserPersonalDetailByUserID(user.Id);

                            if (userPersonalDetailFound != null)
                            {
                                UserInfoViewModel userInfoViewModel = new UserInfoViewModel();
                                userInfoViewModel.UserID = user.Id;
                                userInfoViewModel.UserName = $"{ userPersonalDetailFound.FirstName} {userPersonalDetailFound.LastName}";
                                userInfoViewModel.IsActiveUser = userPersonalDetailFound.IsActive;
                                userInfoViewModel.UserPersonalDetailID = userPersonalDetailFound.UserPersonalDetailID;
                                userInfoViewModel.CompanyID = userPersonalDetailFound.CompanyID ?? 0;
                                userInfoViewModel.UserNo = userPersonalDetailFound.UserNo;
                                HttpContext.Session.SetString("UserInfo", JsonConvert.SerializeObject(userInfoViewModel));
                            }
                        }
                        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "User"))
                        {
                            //return RedirectToAction("Index", "Home", new { Area = "User" });
                            return RedirectToAction("Transfers", "Remittance", new { Area = "User" });
                        }
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            /* Transfer Details */
            //var list = GetAllCurrencies().Result.Where(c => c.CurrencyCode == "AUD");
            //model.LocalCurrency = list.ToList();
            //model.ForeignCurrency = (await GetAllCurrencies()).Where(x => x.CurrencyCode == "PKR").ToList();

            //model.LocalCurrency = (await GetAllCurrencies()).Where(x => x.IsActiveSrcCurrency == true).ToList();
            //model.ForeignCurrency = (await GetAllCurrencies()).Where(x => x.IsActiveDestCurrency == true).ToList();

            /* Personal Details */
            model.BirthCountries = await GetAllCountries();
            //model.PropertyUnitTypes = GetAllUnitTypes();
            //model.StreetTypes = GetAllStreetTypes();
            model.States = GetAllStates();
            model.SecurityQuestions = GetAllSecurityQuestions();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //RegisterViewModel model = new RegisterViewModel();
            /*
             1- Create User Account in AspNetUser table when account create successfully then
             2. Add User Transfer Details information
             3. Add User Personal Details
             4. Add User Document Details
              */
            string msg = string.Empty;

            //if (!string.IsNullOrWhiteSpace(model.HomeAddress) && (string.IsNullOrWhiteSpace(model.StreetNumber) || string.IsNullOrWhiteSpace(model.StreetName) ||
            //   string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString())))
            //{
            //    ModelState.Remove("StreetNumber");
            //    ModelState.Remove("StreetName");
            //    ModelState.Remove("StreetTypeID");
            //    ModelState.Remove("StateID");
            //    ModelState.Remove("City");
            //    ModelState.Remove("PostalCode");
            //}
            //else if (!string.IsNullOrWhiteSpace(model.StreetNumber) || !string.IsNullOrWhiteSpace(model.StreetName) ||
            //    !string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString()))
            //{
            //    ModelState.Remove("HomeAddress");
            //}

            if (!ModelState.IsValid)
            {
                try
                {
                    var user = new ApplicationUser { UserName = model.UserName, Email = model.UserName, SecurityQuestionID = model.SecurityQuestionID, SecurityAnswer = model.SecurityAnswer };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        //Generate Token
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        //var encodeToken = HttpUtility.UrlEncode(token);

                        string confimLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                        var emailList = new List<string>() { model.UserName };

                        //var emailResponse = await _emailSender.SendEmailAsync(emailList, "Email Confirmation Link", confimLink);
                        //var emailResponse = await _emailSender.SendEmailAsync(emailList, "Email Confirmation Link", HtmlEmailTemplates.AccountConfirmationEmailTemplate($"{model.FirstName} {model.LastName}", confimLink));
                        new EmailHelper(_config).SendEmailBySMTP(emailList, "Email Confirmation Link", HtmlEmailTemplates.AccountConfirmationEmailTemplate($"{model.FirstName} {model.LastName}", confimLink));

                        // Add User Personal Details
                        UserPersonalDetail newUserPersonalDetail = new UserPersonalDetail();
                        newUserPersonalDetail.Title = model.Title;
                        newUserPersonalDetail.FirstName = model.FirstName;
                        //newUserPersonalDetail.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? "MiddleName" : model.MiddleName;
                        newUserPersonalDetail.MiddleName = model.MiddleName;
                        newUserPersonalDetail.LastName = model.LastName;
                        newUserPersonalDetail.DateOfBirth = model.DateOfBirth;
                        newUserPersonalDetail.PrimaryContactNo = model.PrimaryContactNo;
                        newUserPersonalDetail.BirthCountryID = model.BirthCountryID;
                        //newUserPersonalDetail.HomeAddress = string.IsNullOrWhiteSpace(model.HomeAddress) ? "HomeAddress" : model.HomeAddress;
                        //newUserPersonalDetail.PropertyName = string.IsNullOrWhiteSpace(model.PropertyName) ? "PropertyName" : model.PropertyName;
                        //newUserPersonalDetail.PropertyUnitTypeID = model.PropertyUnitTypeID;
                        //newUserPersonalDetail.UnitNumber = string.IsNullOrWhiteSpace(model.UnitNumber) ? "UnitNumber" : model.UnitNumber;
                        //newUserPersonalDetail.StreetNumber = string.IsNullOrWhiteSpace(model.StreetNumber) ? "StreetNumber" : model.StreetNumber;
                        //newUserPersonalDetail.StreetName = string.IsNullOrWhiteSpace(model.StreetName) ? "StreetName" : model.StreetName;
                        //newUserPersonalDetail.StreetTypeID = string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString()) ? 1 : model.StreetTypeID;
                        newUserPersonalDetail.StreetAddress = model.StreetAddress;
                        newUserPersonalDetail.StateID = string.IsNullOrWhiteSpace(model.StateID?.ToString()) ? 1 : model.StateID;
                        newUserPersonalDetail.City = string.IsNullOrWhiteSpace(model.City) ? "City" : model.City;
                        newUserPersonalDetail.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? "PostalCode" : model.PostalCode;
                        newUserPersonalDetail.UserId = user.Id;
                        newUserPersonalDetail.IsAgreeToSharePersonalDetails = model.IsAgreeToSharePersonalDetailsCheckboxChecked;
                        newUserPersonalDetail.IsAgreeTermsAndCondition = model.IsTermAndConditionCheckboxChecked;
                        // Get Default CompanyID
                        //newUserPersonalDetail.CompanyID = (await _companyRepository.Get(9)).CompnayID;

                        //IsSuperCompany is always true
                        // Etihad Forex company
                        newUserPersonalDetail.CompanyID = (await _companyRepository.GetAll()).FirstOrDefault(x => x.IsSuperCompany == true).CompanyID;
                        newUserPersonalDetail.CreatedDate = DateTime.Now;
                        newUserPersonalDetail.UpdatedDate = DateTime.Now;

                        // Additional Fields
                        //Code = "R00123",
                        newUserPersonalDetail.Code = Guid.NewGuid().ToString("N").Substring(0, 10);
                        newUserPersonalDetail.CustomerType = CustomerType.Regular;
                        newUserPersonalDetail.PersonTitle = PersonTitle.Mr;
                        //newUserPersonalDetail.OtherName = model.LastName;
                        newUserPersonalDetail.Gender = Gender.Male;
                        newUserPersonalDetail.Occupation = model.Occupation;
                        newUserPersonalDetail.TFN = null;
                        newUserPersonalDetail.IsActive = false;
                        newUserPersonalDetail.Email = model.UserName; // UserName is basically email
                        newUserPersonalDetail.Phone = model.PrimaryContactNo;
                        //newUserPersonalDetail.AddressLine1 = model.StreetName;
                        //newUserPersonalDetail.AddressLine2 = model.StreetName;
                        //newUserPersonalDetail.AddressLine3 = model.StreetName;
                        newUserPersonalDetail.PostZipCode = model.PostalCode;
                        newUserPersonalDetail.PostalRegion = model.PostalCode;
                        newUserPersonalDetail.CountryId = model.BirthCountryID;
                        newUserPersonalDetail.CitizenshipId = null;
                        newUserPersonalDetail.ResidenceId = null;
                        newUserPersonalDetail.BirthPlace = null;
                        newUserPersonalDetail.NationalId = model.NationalCountryID;
                        newUserPersonalDetail.Notes = null;
                        newUserPersonalDetail.SanctionStatus = SanctionStatus.SanctionStatus1;
                        newUserPersonalDetail.SanctionComment = null;
                        newUserPersonalDetail.BlackListComment = null;
                        newUserPersonalDetail.IsBlackListed = false;
                        newUserPersonalDetail.IsAllowedForTransaction = true;

                        var userPersonalDetailResult = await _userPersonalDetailRepository.Add(newUserPersonalDetail);

                        // Add User Transfer Detail
                        //UserTransferDetail newUserTransferDetail = new UserTransferDetail
                        //{
                        //    LocalCurrencyID = Convert.ToInt32(model.LocalCurrencyID),
                        //    ForeignCurrencyID = Convert.ToInt32(model.ForeignCurrencyID),
                        //    UserPersonalDetailID = userPersonalDetailResult.UserPersonalDetailID
                        //};
                        //var userTransferDetailResult = await _userTransferDetailRepository.Add(newUserTransferDetail);

                        if (model != null && model.userDocumentDetailViewModelList != null)
                            foreach (var item in model.userDocumentDetailViewModelList)
                            {
                                UserDocumentDetail newUserDocumentDetail = new UserDocumentDetail
                                {
                                    //IdDocument = item.IdDocument,
                                    //IdDocumentNo = item.IdDocumentNo,
                                    DocumentType = item.DocumentType,
                                    DocumentNo = item.DocumentNo,
                                    OtherDocumentTitle = item.OtherDocumentTitle,
                                    ExpirationDate = Convert.ToDateTime(item.ExpirationDate),
                                    UserPersonalDetailID = userPersonalDetailResult.UserPersonalDetailID
                                };

                                var userDocumentDetailResult = await _userDocumentDetailRepository.Add(newUserDocumentDetail);

                                //Add Document 
                                Document newDocument = new Document();
                                MemoryStream ms = new MemoryStream();
                                //await doc.CopyToAsync(ms);
                                await item.Document.CopyToAsync(ms);
                                newDocument.Data = ms.ToArray();
                                //newDocument.FileName = doc.FileName;
                                newDocument.FileName = item.Document.FileName;
                                //newDocument.FileExtension = Path.GetExtension(doc.FileName);
                                newDocument.FileExtension = Path.GetExtension(item.Document.FileName);
                                newDocument.UserDocumentDetailID = userDocumentDetailResult.UserDocumentDetailID;
                                await _documentRepository.Add(newDocument);


                            }
                        isSuccess = true;
                        msg = "Before you can Login, please confirm your email, by clicking on the " +
                                           "confirmation link we have emailed you";

                        if (_signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
                        {
                            return RedirectToAction("index", "Home");
                        }
                            var userRoleResult = await _userManager.AddToRoleAsync(user, "User");

                        ViewBag.Title = "Registration Successful";
                        ViewBag.Message = "Before you can Login, please confirm your email, by clicking on the " +
                                           "confirmation link we have emailed you";
                        //return View("MessageView");
                    }
                    else
                    {
                        isWarning = true;
                        string errorMsg = "";
                        foreach (var error in result.Errors)
                            errorMsg = error.Description + " " + errorMsg;
                        msg = errorMsg;//"Login not created, please contact system administrator.";
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    isSuccess = false;
                    msg = "An Exception has been occured, please contact system administrator.";
                }
            }

            RegisterViewModel registerViewModel = new RegisterViewModel();
            /* Transfer Details */
            //registerViewModel.LocalCurrency = (await GetAllCurrencies()).ToList();
            //registerViewModel.ForeignCurrency = (await GetAllCurrencies()).ToList();

            /* Personal Details */
            registerViewModel.BirthCountries = await GetAllCountries();
            //registerViewModel.PropertyUnitTypes = GetAllUnitTypes();
            //registerViewModel.StreetTypes = GetAllStreetTypes();
            registerViewModel.States = GetAllStates();
            registerViewModel.SecurityQuestions = GetAllSecurityQuestions();

            //return View(registerViewModel);

            var jsonResult = new
            {
                isSuccess,
                msg
            };

            return Json(jsonResult);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            EditRegisterViewModel editModel = new EditRegisterViewModel();

            UserInfoViewModel userInfoViewModel = new UserInfoViewModel();
            string userInfo = _httpContextAccessor.HttpContext.Session.GetString("UserInfo");
            if (userInfo != null)
            {
                userInfoViewModel = JsonConvert.DeserializeObject<UserInfoViewModel>(userInfo);
            }

            //var user = await _userManager.FindByIdAsync(userInfoViewModel.UserID);
            //if (user != null)
            //if (true)
            //{
            // Add User Personal Details

            // Get information from user table
            //editModel.UserName = user.UserName;
            //editModel.Password = user.PasswordHash;
            //editModel.SecurityAnswer = user.SecurityAnswer;
            //editModel.SecurityQuestionID = user.SecurityQuestionID;


            //var userPersonalDetailFound = (await _userPersonalDetailRepository.GetAll()).FirstOrDefault(x => x.UserPersonalDetailID == userInfoViewModel.UserPersonalDetailID);
            var userPersonalDetailFound = await _userPersonalDetailRepository.Get(userInfoViewModel.UserPersonalDetailID);
            if (userPersonalDetailFound != null)
            {
                editModel.UserPersonalDetailID = userPersonalDetailFound.UserPersonalDetailID;
                editModel.Title = userPersonalDetailFound.Title;
                editModel.FirstName = userPersonalDetailFound.FirstName;
                editModel.MiddleName = string.IsNullOrWhiteSpace(userPersonalDetailFound.MiddleName) ? "MiddleName" : userPersonalDetailFound.MiddleName;
                editModel.LastName = userPersonalDetailFound.LastName;

                editModel.DateOfBirth = userPersonalDetailFound.DateOfBirth;
                editModel.PrimaryContactNo = userPersonalDetailFound.PrimaryContactNo;

                //editModel.HomeAddress = string.IsNullOrWhiteSpace(userPersonalDetailFound.HomeAddress) ? "HomeAddress" : userPersonalDetailFound.HomeAddress;
                //editModel.PropertyName = string.IsNullOrWhiteSpace(userPersonalDetailFound.PropertyName) ? "PropertyName" : userPersonalDetailFound.PropertyName;
                //editModel.PropertyUnitTypeID = userPersonalDetailFound.PropertyUnitTypeID;
                //editModel.UnitNumber = string.IsNullOrWhiteSpace(userPersonalDetailFound.UnitNumber) ? "UnitNumber" : userPersonalDetailFound.UnitNumber;
                //editModel.StreetNumber = string.IsNullOrWhiteSpace(userPersonalDetailFound.StreetNumber) ? "StreetNumber" : userPersonalDetailFound.StreetNumber;
                //editModel.StreetName = string.IsNullOrWhiteSpace(userPersonalDetailFound.StreetName) ? "StreetName" : userPersonalDetailFound.StreetName;
                //editModel.StreetTypeID = string.IsNullOrWhiteSpace(userPersonalDetailFound.StreetTypeID?.ToString()) ? 1 : userPersonalDetailFound.StreetTypeID;
                editModel.StateID = userPersonalDetailFound.StateID;
                editModel.City = userPersonalDetailFound.City;
                editModel.PostalCode = userPersonalDetailFound.PostalCode;
                //editModel.UserId = user.Id;
                editModel.NationalCountryID = userPersonalDetailFound.NationalId;
                editModel.BirthCountryID = userPersonalDetailFound.BirthCountryID;
                editModel.Occupation = userPersonalDetailFound.Occupation;
                editModel.StreetAddress = userPersonalDetailFound.StreetAddress;
                //editModel.CompanyID = null;
                //editModel.CreatedDate = DateTime.Now;
                //editModel.UpdatedDate = DateTime.Now;
            }

            var userDocumentDetails = (await _userDocumentDetailRepository.GetAll())
                .Where(x => x.UserPersonalDetailID == userPersonalDetailFound.UserPersonalDetailID);

            var uniqueDocuments = userDocumentDetails.GroupBy(x => x.DocumentType)
                .Select(s => s.OrderByDescending(y => y.CreatedDate).FirstOrDefault());

            if (userDocumentDetails != null)
            {
                //foreach (var item in userDocumentDetails)
                foreach (var item in uniqueDocuments)
                {
                    UserDocumentDetailViewModel userDocumentDetailViewModel = new UserDocumentDetailViewModel();
                    userDocumentDetailViewModel.UserDocumentDetailID = item.UserDocumentDetailID;
                    userDocumentDetailViewModel.DocumentType = item.DocumentType;
                    userDocumentDetailViewModel.DocumentNo = item.DocumentNo;
                    userDocumentDetailViewModel.ExpirationDate = Convert.ToDateTime(item.ExpirationDate).ToString("yyyy-MM-dd");
                    editModel.userDocumentDetailViewModelList.Add(userDocumentDetailViewModel);
                }
            }
            //editModel.LocalCurrency = (await GetAllCurrencies()).ToList();
            //editModel.ForeignCurrency = (await GetAllCurrencies()).ToList();

            /* Personal Details */
            editModel.BirthCountries = await GetAllCountries();
            //editModel.PropertyUnitTypes = GetAllUnitTypes();
            //editModel.StreetTypes = GetAllStreetTypes();
            editModel.States = GetAllStates();
            //editModel.SecurityQuestions = GetAllSecurityQuestions();
            return View(editModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditRegisterViewModel model)
        {
            //RegisterViewModel model = new RegisterViewModel();
            /*
             1- Create User Account in AspNetUser table when account create successfully then
             2. Add User Transfer Details information
             3. Add User Personal Details
             4. Add User Document Details
              */
            try
            {
                var userPersonalDetailFound = await _userPersonalDetailRepository.Get(model.UserPersonalDetailID);
                if (userPersonalDetailFound != null)
                {
                    //userPersonalDetailFound.Title = model.Title;
                    //userPersonalDetailFound.FirstName = model.FirstName;
                    //userPersonalDetailFound.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? "MiddleName" : model.MiddleName;
                    //userPersonalDetailFound.LastName = model.LastName;
                    //userPersonalDetailFound.DateOfBirth = model.DateOfBirth;
                    userPersonalDetailFound.PrimaryContactNo = model.PrimaryContactNo;
                    //userPersonalDetailFound.NationalId = model.NationalCountryID;
                    //userPersonalDetailFound.BirthCountryID = model.BirthCountryID;
                    //userPersonalDetailFound.HomeAddress = string.IsNullOrWhiteSpace(model.HomeAddress) ? "HomeAddress" : model.HomeAddress;
                    //userPersonalDetailFound.PropertyName = string.IsNullOrWhiteSpace(model.PropertyName) ? "PropertyName" : model.PropertyName;
                    //userPersonalDetailFound.PropertyUnitTypeID = model.PropertyUnitTypeID;
                    //userPersonalDetailFound.UnitNumber = string.IsNullOrWhiteSpace(model.UnitNumber) ? "UnitNumber" : model.UnitNumber;
                    //userPersonalDetailFound.StreetNumber = string.IsNullOrWhiteSpace(model.StreetNumber) ? "StreetNumber" : model.StreetNumber;
                    //userPersonalDetailFound.StreetName = string.IsNullOrWhiteSpace(model.StreetName) ? "StreetName" : model.StreetName;
                    //userPersonalDetailFound.StreetTypeID = string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString()) ? 1 : model.StreetTypeID;
                    userPersonalDetailFound.StateID = string.IsNullOrWhiteSpace(model.StateID?.ToString()) ? 1 : model.StateID;
                    userPersonalDetailFound.City = string.IsNullOrWhiteSpace(model.City) ? "City" : model.City;
                    userPersonalDetailFound.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? "PostalCode" : model.PostalCode;
                    userPersonalDetailFound.UpdatedDate = DateTime.Now;
                    //userPersonalDetailFound.CreatedDate = DateTime.Now;

                    var userPersonalDetailResultUpdated = await _userPersonalDetailRepository.Update(userPersonalDetailFound);
                    if (userPersonalDetailResultUpdated != null)
                    {
                        isSuccess = true;
                        message = "Record updated successfully";
                    }
                    else
                    {
                        isWarning = true;
                        message = "Record not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = ex.Message;
            }

            var jsonResult = new
            {
                isWarning,
                isSuccess,
                message
            };

            return Json(jsonResult);
        }

        [HttpPost]
        public async Task<JsonResult> AddUserDocument(UserDocumentDetailViewModel addModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    UserDocumentDetail newUserDocumentDetail = new UserDocumentDetail();
                    newUserDocumentDetail.DocumentType = addModel.DocumentType;
                    newUserDocumentDetail.DocumentNo = addModel.DocumentNo;
                    newUserDocumentDetail.ExpirationDate = Convert.ToDateTime(addModel.ExpirationDate);
                    newUserDocumentDetail.UserDocumentDetailID = addModel.UserDocumentDetailID;
                    newUserDocumentDetail.UserPersonalDetailID = addModel.UserPersonalDetailID;
                    newUserDocumentDetail.OtherDocumentTitle = addModel.OtherDocumentTitle;
                    newUserDocumentDetail.CreatedDate = DateTime.Now;
                    newUserDocumentDetail.UpdatedDate = DateTime.Now;

                    var userDocumentDetailAdded = await _userDocumentDetailRepository.Add(newUserDocumentDetail);
                    if (userDocumentDetailAdded != null)
                    {
                        isSuccess = true;
                        message = "Document added successfully";
                        //Add Document here
                        Document newDocument = new Document();
                        MemoryStream ms = new MemoryStream();
                        //await doc.CopyToAsync(ms);
                        await addModel.Document.CopyToAsync(ms);
                        newDocument.Data = ms.ToArray();
                        //newDocument.FileName = doc.FileName;
                        newDocument.FileName = addModel.Document.FileName;
                        //newDocument.FileExtension = Path.GetExtension(doc.FileName);
                        newDocument.FileExtension = Path.GetExtension(addModel.Document.FileName);
                        newDocument.UserDocumentDetailID = userDocumentDetailAdded.UserDocumentDetailID;
                        newDocument.CreatedDate = DateTime.Now;
                        newDocument.UpdatedDate = DateTime.Now;
                        var documentAdded = await _documentRepository.Add(newDocument);
                        if (documentAdded != null)
                        {
                            isSuccess = true;
                            message = "Document successfully added";
                        }
                    }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    message = ex.Message;
                }

            }
            var jsonResult = new
            {
                isSuccess,
                isWarning,
                message
            };
            return Json(jsonResult);
        }

        [HttpPost]
        public async Task<JsonResult> DeleteUserDocument(int userDocumentDetailID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userDocumentDetailFound = await _userDocumentDetailRepository.Get(userDocumentDetailID);
                    if (userDocumentDetailFound != null)
                    {
                        isSuccess = true;
                        message = "Document deleted successfully";
                        //Delete Document here
                        var docs = await _documentRepository.GetDocumentsByUserDocumentDetailID(userDocumentDetailID);
                        if (docs != null)
                        {
                            foreach (var item in docs)
                            {
                                var documentDeleted = await _documentRepository.Delete(item.DocumentID);
                                if (documentDeleted != null)
                                {
                                    isSuccess = true;
                                    message = "Document successfully added";
                                }
                            }
                            var userDocumentDetailDeleted = await _userDocumentDetailRepository.Delete(userDocumentDetailID);
                            if (userDocumentDetailDeleted != null)
                            {
                                isSuccess = true;
                                message = "UserDocumentDetail deleted successfully";
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    message = ex.Message;
                }

            }
            var jsonResult = new
            {
                isSuccess,
                isWarning,
                message
            };
            return Json(jsonResult);
        }


        [HttpGet]
        public async Task<JsonResult> ViewCustomerDocument(int userDocumentDetailID)
        {
            string base64StringSrc = string.Empty;
            try
            {
                var documentsCollection = (await _documentRepository.GetDocumentsByUserDocumentDetailID(userDocumentDetailID));
                if (documentsCollection != null)
                {
                    foreach (var item in documentsCollection)
                    {
                        isSuccess = true;
                        message = "Receipt Found";
                        string imgString = CommonExtension.ByteArrayToImage(item.Data);
                        if (item.FileExtension.ToLower() == ".png")
                        {
                            base64StringSrc = "data:image/png;base64," + imgString;
                        }
                        else if (item.FileExtension.ToLower() == ".jpg" || item.FileExtension.ToLower() == ".jpeg")
                        {
                            base64StringSrc = "data:image/jpg;base64," + imgString;
                        }
                        else if (item.FileExtension.ToLower() == ".gif")
                        {
                            base64StringSrc = "data:image/gif;base64," + imgString;
                        }
                        else if (item.FileExtension.ToLower() == ".pdf")
                        {
                            base64StringSrc = "data:application/pdf;base64," + imgString;
                        }
                    }
                }
                else
                {
                    isWarning = false;
                    message = "Document not found";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                isSuccess = false;
            }

            var jsonResult = new
            {
                isSuccess,
                isWarning,
                message,
                base64StringSrc
            };
            return Json(jsonResult);
        }

        #region Register post method 

        //[HttpPost]
        //public async Task<IActionResult> Register(RegisterViewModel model)
        //{
        //    /*
        //     1- Create User Account in AspNetUser table when account create successfully then
        //     2. Add User Transfer Details information
        //     3. Add User Personal Details
        //     4. Add User Document Details
        //      */

        //    if (!string.IsNullOrWhiteSpace(model.HomeAddress) && (string.IsNullOrWhiteSpace(model.StreetNumber) || string.IsNullOrWhiteSpace(model.StreetName) ||
        //       string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString())))
        //    {
        //        ModelState.Remove("StreetNumber");
        //        ModelState.Remove("StreetName");
        //        ModelState.Remove("StreetTypeID");
        //        ModelState.Remove("StateID");
        //        ModelState.Remove("City");
        //        ModelState.Remove("PostalCode");
        //    }
        //    else if (!string.IsNullOrWhiteSpace(model.StreetNumber) || !string.IsNullOrWhiteSpace(model.StreetName) ||
        //        !string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString()))
        //    {
        //        ModelState.Remove("HomeAddress");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            RegisterViewModel model1 = new RegisterViewModel();
        //            var user = new ApplicationUser { UserName = model.UserName, Email = model.UserName, SecurityQuestionID = model.SecurityQuestionID, SecurityAnswer = model.SecurityAnswer };

        //            var result = await _userManager.CreateAsync(user, model.Password);
        //            if (result.Succeeded)
        //            {
        //                //Generate Token
        //                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //                //var encodeToken = HttpUtility.UrlEncode(token);

        //                string confimLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);
        //                //await _emailService.Send(new EmailMessage
        //                //{
        //                //    Subject = "Email Confirmation Link",
        //                //    //Content = confirmationLink,
        //                //    Content = confimLink,
        //                //    FromAddresses = new List<EmailAddress>
        //                //{
        //                //    new EmailAddress{ Name = "Naveed",Address = "devnaveed1416@gmail.com"}
        //                //},
        //                //    ToAddresses = new List<EmailAddress>
        //                //{
        //                //    //new EmailAddress{ Name = "Naveed",Address = "nalam@tekhqs.com"}
        //                //    new EmailAddress{ Name = $"{model.FirstName} {model.MiddleName} {model.LastName}",Address = model.UserName}
        //                //}});

        //                //var emailList = new List<string>() { "forexetihad@gmail.com" };
        //                var emailList = new List<string>() { model.UserName };

        //               var emailResponse = await _emailSender.SendEmailAsync(emailList, "Email Confirmation Link", confimLink);

        //                // Add User Personal Details
        //                UserPersonalDetail newUserPersonalDetail = new UserPersonalDetail
        //                {
        //                    Title = model.PersonTitle?.ToString(),
        //                    FirstName = model.FirstName,
        //                    MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? "MiddleName" : model.MiddleName,
        //                    LastName = model.LastName,
        //                    DateOfBirth = model.DateOfBirth,
        //                    PrimaryContactNo = model.PrimaryContactNo,
        //                    BirthCountryID = model.BirthCountryID,
        //                    HomeAddress = string.IsNullOrWhiteSpace(model.HomeAddress) ? "HomeAddress": model.HomeAddress,
        //                    PropertyName = string.IsNullOrWhiteSpace(model.PropertyName) ? "PropertyName" : model.PropertyName,
        //                    PropertyUnitTypeID = model.PropertyUnitTypeID,
        //                    UnitNumber = string.IsNullOrWhiteSpace(model.UnitNumber) ? "UnitNumber" : model.UnitNumber,
        //                    StreetNumber = string.IsNullOrWhiteSpace(model.StreetNumber) ? "StreetNumber" : model.StreetNumber,
        //                    StreetName = string.IsNullOrWhiteSpace(model.StreetName) ? "StreetName" : model.StreetName,
        //                    StreetTypeID = string.IsNullOrWhiteSpace(model.StreetTypeID?.ToString()) ? 1 : model.StreetTypeID,
        //                    StateID = string.IsNullOrWhiteSpace(model.StateID?.ToString()) ? 1 : model.StateID,
        //                    City = string.IsNullOrWhiteSpace(model.City) ? "City" : model.City,
        //                    PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? "PostalCode" : model.PostalCode,
        //                    UserId = user.Id,

        //                    // Get Default CompanyID
        //                    CompanyID = (await _companyRepository.Get(9)).CompnayID,
        //                    CreatedDate = DateTime.Now,
        //                    UpdatedDate = DateTime.Now,

        //                    // Additional Fields
        //                    //Code = "R00123",
        //                    Code = Guid.NewGuid().ToString("N").Substring(0,10),
        //                    CustomerType = CustomerType.Regular,
        //                    PersonTitle = (PersonTitle)model.PersonTitle,
        //                    OtherName = model.LastName,
        //                    Gender = Gender.Male,
        //                    Occupation = model.Occupation,
        //                    SSNType = SSNType.None,
        //                    SSN = SSNType.None.ToString(),
        //                    IsActive = false,
        //                    Email = model.UserName, // UserName is basically email
        //                    Phone = model.PrimaryContactNo,
        //                    AddressLine1 = model.StreetName,
        //                    AddressLine2 = model.StreetName,
        //                    AddressLine3 = model.StreetName,
        //                    PostZipCode = model.PostalCode,
        //                    PostalRegion = model.PostalCode,
        //                    CountryId = model.BirthCountryID,
        //                    CitizenshipId = null,
        //                    ResidenceId = null,
        //                    BirthPlace = null,
        //                    NationalId = model.NationalCountryID,
        //                    Notes = null,
        //                    SanctionStatus = SanctionStatus.SanctionStatus1,
        //                    SanctionComment = null,
        //                    BlackListComment = null,
        //                    IsBlackListed = false,
        //                    IsAllowedForTransaction = true

        //                };
        //                var userPersonalDetailResult = await _userPersonalDetailRepository.Add(newUserPersonalDetail);


        //                //var newCustomer = new Customer()
        //                //{
        //                //    PersonTitle = (PersonTitle)model.PersonTitle,
        //                //    FirstName = model.FirstName,
        //                //    LastName = model.LastName,
        //                //    OtherName = model.MiddleName,
        //                //    //Gender
        //                //    DateOfBirth = model.DateOfBirth,
        //                //    CompanyId = (await _companyRepository.Get(9)).CompnayID,
        //                //    UserPersonalDetailId = userPersonalDetailResult.UserPersonalDetailID,
        //                //    //Occupation
        //                //    //SSNType
        //                //    //SSN
        //                //    Email = model.UserName,
        //                //    Phone = model.PrimaryContactNo,

        //                //};

        //                // Add User Transfer Detail
        //                UserTransferDetail newUserTransferDetail = new UserTransferDetail
        //                {
        //                    LocalCurrencyID = (int)model.LocalCurrencyID,
        //                    ForeignCurrencyID = (int)model.ForeignCurrencyID,
        //                    UserPersonalDetailID = userPersonalDetailResult.UserPersonalDetailID
        //                };
        //                var userTransferDetailResult = await _userTransferDetailRepository.Add(newUserTransferDetail);

        //                UserDocumentDetail newUserDocumentDetail = new UserDocumentDetail
        //                {
        //                    IdDocument = (int)model.IdDocument,
        //                    IdDocumentNo = model.IdDocumentNo,
        //                    //CountryID = model.NationalCountryID,
        //                    //Occupation = model.Occupation,
        //                    UserPersonalDetailID = userPersonalDetailResult.UserPersonalDetailID
        //                };
        //                var userDocumentDetailResult = await _userDocumentDetailRepository.Add(newUserDocumentDetail);
        //                //Add Document 
        //                if (model.Documents != null && model.Documents.Count() > 0)
        //                    foreach (var doc in model.Documents)
        //                    {
        //                        Document newDocument = new Document();
        //                        MemoryStream ms = new MemoryStream();
        //                        await doc.CopyToAsync(ms);
        //                        newDocument.Data = ms.ToArray();
        //                        newDocument.FileName = doc.FileName;
        //                        newDocument.FileExtension = Path.GetExtension(doc.FileName);
        //                        newDocument.UserDocumentDetailID = userDocumentDetailResult.UserDocumentDetailID;
        //                        await _documentRepository.Add(newDocument);
        //                    }
        //                if (_signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
        //                {
        //                    return RedirectToAction("index", "Home");
        //                }

        //                if(user?.UserName?.ToLower() == "alivirk@etihadforex.com.au" || user?.UserName?.ToLower() == "naveedmahsud@gmail.com")
        //                {
        //                    var userRoleResult = await _userManager.AddToRoleAsync(user, "SuperAdmin");
        //                }
        //                else
        //                {
        //                    var userRoleResult = await _userManager.AddToRoleAsync(user, "User");
        //                }

        //                ViewBag.Title = "Registration Successful";
        //                ViewBag.Message = "Before you can Login, please confirm your email, by clicking on the " +
        //                                   "confirmation link we have emailed you";
        //                return View("MessageView");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex.Message);
        //        }
        //    }

        //    RegisterViewModel registerViewModel = new RegisterViewModel();
        //    /* Transfer Details */
        //    registerViewModel.LocalCurrency = (await GetAllCurrencies()).ToList();
        //    registerViewModel.ForeignCurrency = (await GetAllCurrencies()).ToList();

        //    /* Personal Details */
        //    registerViewModel.BirthCountries = await GetAllCountries();
        //    registerViewModel.PropertyUnitTypes = GetAllUnitTypes();
        //    registerViewModel.StreetTypes = GetAllStreetTypes();
        //    registerViewModel.States = GetAllStates();
        //    registerViewModel.SecurityQuestions = GetAllSecurityQuestions();

        //    return View(registerViewModel);
        //}
        #endregion


        public async Task<IActionResult> Profile()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("index", "home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"The User ID {userId} is invalid";
                return View("NotFound");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View();
            }
            ViewBag.ErrorTitle = "Email cannot be confirmed";
            return View("Error");

        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        //[HttpPost, AllowAnonymous, FormValidator]
        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            //string msg = string.Empty;
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        //var encodedToken = HttpUtility.UrlEncode(token);
                        string passwordResetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token = token }, Request.Scheme);

                        //await _emailService.Send(new EmailMessage
                        //{
                        //    Subject = "Password Reset Link",
                        //    Content = passwordResetLink,
                        //    FromAddresses = new List<EmailAddress>
                        //{
                        //    new EmailAddress{ Name = "Naveed",Address = "devnaveed1416@gmail.com"}
                        //},
                        //    ToAddresses = new List<EmailAddress>
                        //{
                        //    new EmailAddress{ Name = $"{user.UserName}",Address = model.Email}
                        //}
                        //});
                        var emailList = new List<string>() { model.Email };
                        var emailResponse = await _emailSender.SendEmailAsync(emailList, "Message From EtihadForex", HtmlEmailTemplates.AccountResetEmailTemplate(passwordResetLink));
                        message = "If you have an account with us, we have sent an email with the instructions to reset your password.";
                        isSuccess = emailResponse?.StatusCode.ToString() == "Accepted" ? true : false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    message = "An exception has been occured";
                    isSuccess = false;
                }
                //return FormResult.CreateSuccessResult(msg);
            }
            else
            {
                message = "Invalid ModalState";
                isSuccess = false;
            }
            var jsonResult = new
            {
                isSuccess,
                message
            };
            return Json(jsonResult);
            //return FormResult.CreateErrorResult(msg);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid password reset token");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            ModelState.Remove("OldPassword");
            if (ModelState.IsValid)
            {
                try
                {

                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                        if (result.Succeeded)
                        {
                            model.IsPasswordReset = true;
                            ViewBag.Title = "";
                            ViewBag.Login = "<div class='mb-3 pl-4'><a href='/Account/Login'>Click here to login</a></div>";
                            ModelState.AddModelError("SuccessKey", "Your password is reset");
                        }
                        else
                        {
                            model.IsPasswordReset = false;
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost, AjaxOnly]
        public async Task<IActionResult> ChangePassword(ResetPasswordViewModel model)
        {
            //if (ModelState.IsValid)
            if (true)
            {
                try
                {
                   var user = await _userManager.FindByIdAsync(model.UserID);
                   PasswordVerificationResult passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);
                    
                    if(passwordVerificationResult == PasswordVerificationResult.Success)
                    {
                        //var user = await _userManager.FindByEmailAsync(email: email);
                        if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                            var result = await _userManager.ResetPasswordAsync(user: user, token: token, newPassword: model.Password);
                            if (result.Succeeded)
                            {
                                isSuccess = true;
                                message = "Password changed successfully";
                            }
                            else
                            {
                                isWarning = false;
                                message = "Password not changed";
                            }
                        }
                    }
                    else
                    {
                        isWarning = false;
                        message = "Your old password is invalid";
                    }
                    
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    message = ex.Message;
                    logger.LogError(ex.Message);
                }
            }
            else
            {
                isSuccess = true;
                message = "Invalid modal state";
            }
            var jsonResult = new
            {
                isSuccess,
                isWarning,
                message
            };
            return Json(jsonResult);
        }

        [HttpGet]
        [ActionName("NotFound")]
        public IActionResult NotFoundAction()
        {
            return View();
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> IsEmailInUse(string UserName)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(UserName);
                if (user == null)
                {
                    return Json(true);
                }
                else
                {
                    return Json($"{UserName} is already in use");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _httpContextAccessor.HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        private async Task<IEnumerable<CurrencyViewModel>> GetAllCurrencies()
        {
            return (await _currencyRepository.GetAll()).Select(currency => new CurrencyViewModel
            {
                CurrencyID = currency.CurrencyID,
                CurrencyCode = currency.CurrencyCode,
                CurrencyName = currency.CurrencyName,
                ConcatCurrency = $"{currency.CurrencyCode} - {currency.CurrencyName}",
                IsActiveDestCurrency = currency.IsActiveDestCurrency,
                IsActiveSrcCurrency = currency.IsActiveSrcCurrency
            });
        }
        private async Task<IList<AuthenticationScheme>> GetExternalLogins()
        {
            return (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }
        private async Task<IEnumerable<CountryViewModel>> GetAllCountries()
        {
            return (await _countryRepository.GetAll()).Select(x => new CountryViewModel
            {
                CountryID = x.CountryID,
                CountryName = x.CountryName,
                CountryCode = x.CountryCode,
                CurrencyID = x.CurrencyID,
                Currency = new CurrencyViewModel
                {
                    CurrencyCode = x.Currency.CurrencyCode,
                    CurrencyID = x.Currency.CurrencyID,
                    CurrencyName = x.Currency.CurrencyName
                },
                ExistingPhotoPath = x.PhotoPath,
            });
        }

        private IEnumerable<PropertyUnitTypeViewModel> GetAllUnitTypes()
        {
            IEnumerable<PropertyUnitTypeViewModel> propertyUnitTypes = new List<PropertyUnitTypeViewModel>
            {
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 1, PropertyUnitTypeName = "ANTENNA" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 2, PropertyUnitTypeName = "APARTMENT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 3, PropertyUnitTypeName = "AUTOMATED TELLER MACHINE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 4, PropertyUnitTypeName = "BLOCK" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 5, PropertyUnitTypeName = "BOATSHED" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 6, PropertyUnitTypeName = "BUILDING" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 7, PropertyUnitTypeName = "BUNGALOW" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 8, PropertyUnitTypeName = "CAGE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 9, PropertyUnitTypeName = "CARPARK" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 10, PropertyUnitTypeName = "CARSPACE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 11, PropertyUnitTypeName = "CLUB" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 12, PropertyUnitTypeName = "COOLROOM" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 13, PropertyUnitTypeName = "COTTAGE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 14, PropertyUnitTypeName = "DUPLEX" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 15, PropertyUnitTypeName = "FACTORY" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 16, PropertyUnitTypeName = "FLAT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 17, PropertyUnitTypeName = "GARAGE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 18, PropertyUnitTypeName = "HALL" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 19, PropertyUnitTypeName = "HOUSE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 20, PropertyUnitTypeName = "KIOSK" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 21, PropertyUnitTypeName = "LOT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 22, PropertyUnitTypeName = "MAISONETTE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 23, PropertyUnitTypeName = "MARINE BERTH" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 24, PropertyUnitTypeName = "OFFICE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 25, PropertyUnitTypeName = "PENTHOUSE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 26, PropertyUnitTypeName = "REAR" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 27, PropertyUnitTypeName = "RESERVE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 28, PropertyUnitTypeName = "ROOM" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 29, PropertyUnitTypeName = "SECTION" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 30, PropertyUnitTypeName = "SHED" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 31, PropertyUnitTypeName = "SHOP" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 32, PropertyUnitTypeName = "SHOWROOM" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 33, PropertyUnitTypeName = "SIGN" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 34, PropertyUnitTypeName = "SITE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 35, PropertyUnitTypeName = "STALL" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 36, PropertyUnitTypeName = "STORE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 37, PropertyUnitTypeName = "STRATA UNIT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 38, PropertyUnitTypeName = "STUDIO" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 39, PropertyUnitTypeName = "SUBSTATION" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 40, PropertyUnitTypeName = "SUITE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 41, PropertyUnitTypeName = "TENANCY" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 42, PropertyUnitTypeName = "TOWER" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 43, PropertyUnitTypeName = "TOWNHOUSE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 44, PropertyUnitTypeName = "UNIT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 45, PropertyUnitTypeName = "VAULT" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 46, PropertyUnitTypeName = "VILLA" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 47, PropertyUnitTypeName = "WARD" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 48, PropertyUnitTypeName = "WAREHOUSE" },
                new PropertyUnitTypeViewModel { PropertyUnitTypeID = 49, PropertyUnitTypeName = "WORKSHOP" }
            };


            return propertyUnitTypes;
        }
        private IEnumerable<StreetTypeViewModel> GetAllStreetTypes()
        {
            IEnumerable<StreetTypeViewModel> streetTypes = new List<StreetTypeViewModel>
            {
                new StreetTypeViewModel { StreetTypeID = 1, Name = "ACCESS" },
                new StreetTypeViewModel { StreetTypeID = 2, Name = "ALLEY" },
                new StreetTypeViewModel { StreetTypeID = 3, Name = "AMBLE" },
                new StreetTypeViewModel { StreetTypeID = 4, Name = "APPROACH" },
                new StreetTypeViewModel { StreetTypeID = 5, Name = "ARCADE" },
                new StreetTypeViewModel { StreetTypeID = 6, Name = "ARTERY" },
                new StreetTypeViewModel { StreetTypeID = 7, Name = "AVENUE" },
                new StreetTypeViewModel { StreetTypeID = 8, Name = "BANAN" },
                new StreetTypeViewModel { StreetTypeID = 9, Name = "BANK" },
                new StreetTypeViewModel { StreetTypeID = 10, Name = "BAY" },
                new StreetTypeViewModel { StreetTypeID = 11, Name = "BEACH" },
                new StreetTypeViewModel { StreetTypeID = 12, Name = "BEND" },
                new StreetTypeViewModel { StreetTypeID = 13, Name = "BOARDWALK" },
                new StreetTypeViewModel { StreetTypeID = 14, Name = "BOULEVARD" },
                new StreetTypeViewModel { StreetTypeID = 15, Name = "BOWL" },
                new StreetTypeViewModel { StreetTypeID = 16, Name = "BRACE" },
                new StreetTypeViewModel { StreetTypeID = 17, Name = "BRAE" },
                new StreetTypeViewModel { StreetTypeID = 18, Name = "BREAK" },
                new StreetTypeViewModel { StreetTypeID = 19, Name = "BROADWAY" },
                new StreetTypeViewModel { StreetTypeID = 20, Name = "BROW" },
                new StreetTypeViewModel { StreetTypeID = 21, Name = "BUSWAY" },
                new StreetTypeViewModel { StreetTypeID = 22, Name = "BYPASS" },
                new StreetTypeViewModel { StreetTypeID = 23, Name = "CAUSEWAY" },
                new StreetTypeViewModel { StreetTypeID = 24, Name = "CENTRE" },
                new StreetTypeViewModel { StreetTypeID = 25, Name = "CENTREWAY" },
                new StreetTypeViewModel { StreetTypeID = 26, Name = "CHASE" },
                new StreetTypeViewModel { StreetTypeID = 27, Name = "CIRCLE" },
                new StreetTypeViewModel { StreetTypeID = 28, Name = "CIRCUIT" },
                new StreetTypeViewModel { StreetTypeID = 29, Name = "CIRCUS" },
                new StreetTypeViewModel { StreetTypeID = 30, Name = "CLOSE" },
                new StreetTypeViewModel { StreetTypeID = 31, Name = "CLUSTER" },
                new StreetTypeViewModel { StreetTypeID = 32, Name = "COMMON" },
                new StreetTypeViewModel { StreetTypeID = 33, Name = "COMMONS" },
                new StreetTypeViewModel { StreetTypeID = 34, Name = "CONCORD" },
                new StreetTypeViewModel { StreetTypeID = 35, Name = "CONCOURSE" },
                new StreetTypeViewModel { StreetTypeID = 36, Name = "CONNECTION" },
                new StreetTypeViewModel { StreetTypeID = 37, Name = "COPSE" },
                new StreetTypeViewModel { StreetTypeID = 38, Name = "CORNER" },
                new StreetTypeViewModel { StreetTypeID = 39, Name = "CORSO" },
                new StreetTypeViewModel { StreetTypeID = 40, Name = "COURSE" },
                new StreetTypeViewModel { StreetTypeID = 41, Name = "COURT" },
                new StreetTypeViewModel { StreetTypeID = 42, Name = "COURTYARD" },
                new StreetTypeViewModel { StreetTypeID = 43, Name = "COVE" },
                new StreetTypeViewModel { StreetTypeID = 44, Name = "CRESCENT" },
                new StreetTypeViewModel { StreetTypeID = 45, Name = "CREST" },
                new StreetTypeViewModel { StreetTypeID = 46, Name = "CRIEF" },
                new StreetTypeViewModel { StreetTypeID = 47, Name = "CROSS" },
                new StreetTypeViewModel { StreetTypeID = 48, Name = "CROSSING" },
                new StreetTypeViewModel { StreetTypeID = 49, Name = "CRUISEWAY" },
                new StreetTypeViewModel { StreetTypeID = 50, Name = "CUTTING" },
                new StreetTypeViewModel { StreetTypeID = 51, Name = "DALE" },
                new StreetTypeViewModel { StreetTypeID = 52, Name = "DASH" },
                new StreetTypeViewModel { StreetTypeID = 53, Name = "DELL" },
                new StreetTypeViewModel { StreetTypeID = 54, Name = "DENE" },
                new StreetTypeViewModel { StreetTypeID = 55, Name = "DEVIATION" },
                new StreetTypeViewModel { StreetTypeID = 56, Name = "DIP" },
                new StreetTypeViewModel { StreetTypeID = 57, Name = "DISTRIBUTOR" },
                new StreetTypeViewModel { StreetTypeID = 58, Name = "DIVIDE" },
                new StreetTypeViewModel { StreetTypeID = 59, Name = "DRIVEWAY" },
                new StreetTypeViewModel { StreetTypeID = 60, Name = "EASEMENT" },
                new StreetTypeViewModel { StreetTypeID = 61, Name = "EDGE" },
                new StreetTypeViewModel { StreetTypeID = 62, Name = "ELBOW" },
                new StreetTypeViewModel { StreetTypeID = 63, Name = "END" },
                new StreetTypeViewModel { StreetTypeID = 64, Name = "ENTRANCE" },
                new StreetTypeViewModel { StreetTypeID = 65, Name = "ESPLANADE" },
                new StreetTypeViewModel { StreetTypeID = 66, Name = "ESTATE" },
                new StreetTypeViewModel { StreetTypeID = 67, Name = "EXPRESSWAY" },
                new StreetTypeViewModel { StreetTypeID = 68, Name = "FAIRWAY" },
                new StreetTypeViewModel { StreetTypeID = 69, Name = "FIRE TRACK" },
                new StreetTypeViewModel { StreetTypeID = 70, Name = "FIRELINE" },
                new StreetTypeViewModel { StreetTypeID = 71, Name = "FIRETRAIL" },
                new StreetTypeViewModel { StreetTypeID = 72, Name = "FLAT" },
                new StreetTypeViewModel { StreetTypeID = 73, Name = "FOLLOW" },
                new StreetTypeViewModel { StreetTypeID = 74, Name = "FORD" },
                new StreetTypeViewModel { StreetTypeID = 75, Name = "FORESHORE" },
                new StreetTypeViewModel { StreetTypeID = 76, Name = "FORK" },
                new StreetTypeViewModel { StreetTypeID = 77, Name = "FREEWAY" },
                new StreetTypeViewModel { StreetTypeID = 78, Name = "FRONTAGE" },
                new StreetTypeViewModel { StreetTypeID = 79, Name = "GAP" },
                new StreetTypeViewModel { StreetTypeID = 80, Name = "GARDEN" },
                new StreetTypeViewModel { StreetTypeID = 81, Name = "GARDENS" },
                new StreetTypeViewModel { StreetTypeID = 82, Name = "GATE" },
                new StreetTypeViewModel { StreetTypeID = 83, Name = "GATEWAY" },
                new StreetTypeViewModel { StreetTypeID = 84, Name = "GLADE" },
                new StreetTypeViewModel { StreetTypeID = 85, Name = "GLEN" },
                new StreetTypeViewModel { StreetTypeID = 86, Name = "GRANGE" },
                new StreetTypeViewModel { StreetTypeID = 87, Name = "GREEN" },
                new StreetTypeViewModel { StreetTypeID = 88, Name = "GROVE" },
                new StreetTypeViewModel { StreetTypeID = 89, Name = "GULLY" },
                new StreetTypeViewModel { StreetTypeID = 90, Name = "HARBOUR" },
                new StreetTypeViewModel { StreetTypeID = 91, Name = "HAVEN" },
                new StreetTypeViewModel { StreetTypeID = 92, Name = "HEATH" },
                new StreetTypeViewModel { StreetTypeID = 93, Name = "HEIGHTS" },
                new StreetTypeViewModel { StreetTypeID = 94, Name = "HIGHWAY" },
                new StreetTypeViewModel { StreetTypeID = 95, Name = "HILL" },
                new StreetTypeViewModel { StreetTypeID = 96, Name = "HOLLOW" },
                new StreetTypeViewModel { StreetTypeID = 97, Name = "HUB" },
                new StreetTypeViewModel { StreetTypeID = 98, Name = "ISLAND" },
                new StreetTypeViewModel { StreetTypeID = 99, Name = "JUNCTION" },
                new StreetTypeViewModel { StreetTypeID = 100, Name = "KEY" },
                new StreetTypeViewModel { StreetTypeID = 101, Name = "KEYS" },
                new StreetTypeViewModel { StreetTypeID = 102, Name = "LANDING" },
                new StreetTypeViewModel { StreetTypeID = 103, Name = "LANE" },
                new StreetTypeViewModel { StreetTypeID = 104, Name = "LANEWAY" },
                new StreetTypeViewModel { StreetTypeID = 105, Name = "LINE" },
                new StreetTypeViewModel { StreetTypeID = 106, Name = "LINK" },
                new StreetTypeViewModel { StreetTypeID = 107, Name = "LOOKOUT" },
                new StreetTypeViewModel { StreetTypeID = 108, Name = "LOOP" },
                new StreetTypeViewModel { StreetTypeID = 109, Name = "LYNNE" },
                new StreetTypeViewModel { StreetTypeID = 110, Name = "MALL" },
                new StreetTypeViewModel { StreetTypeID = 111, Name = "MANOR" },
                new StreetTypeViewModel { StreetTypeID = 112, Name = "MEAD" },
                new StreetTypeViewModel { StreetTypeID = 113, Name = "MEANDER" },
                new StreetTypeViewModel { StreetTypeID = 114, Name = "MEWS" },
                new StreetTypeViewModel { StreetTypeID = 115, Name = "MOTORWAY" },
                new StreetTypeViewModel { StreetTypeID = 116, Name = "NOOK" },
                new StreetTypeViewModel { StreetTypeID = 117, Name = "OUTLET" },
                new StreetTypeViewModel { StreetTypeID = 118, Name = "OUTLOOK" },
                new StreetTypeViewModel { StreetTypeID = 119, Name = "PARADE" },
                new StreetTypeViewModel { StreetTypeID = 120, Name = "PARK" },
                new StreetTypeViewModel { StreetTypeID = 121, Name = "PARKWAY" },
                new StreetTypeViewModel { StreetTypeID = 122, Name = "PASS" },
                new StreetTypeViewModel { StreetTypeID = 123, Name = "PASSAGE" },
                new StreetTypeViewModel { StreetTypeID = 124, Name = "PATH" },
                new StreetTypeViewModel { StreetTypeID = 125, Name = "PATHWAY" },
                new StreetTypeViewModel { StreetTypeID = 126, Name = "PLACE" },
                new StreetTypeViewModel { StreetTypeID = 127, Name = "PLAZA" },
                new StreetTypeViewModel { StreetTypeID = 128, Name = "POCKET" },
                new StreetTypeViewModel { StreetTypeID = 129, Name = "POINT" },
                new StreetTypeViewModel { StreetTypeID = 130, Name = "PORT" },
                new StreetTypeViewModel { StreetTypeID = 131, Name = "PRECINCT" },
                new StreetTypeViewModel { StreetTypeID = 132, Name = "PROMENADE" },
                new StreetTypeViewModel { StreetTypeID = 133, Name = "PURSUIT" },
                new StreetTypeViewModel { StreetTypeID = 134, Name = "QUADRANT" },
                new StreetTypeViewModel { StreetTypeID = 135, Name = "QUAY" },
                new StreetTypeViewModel { StreetTypeID = 136, Name = "QUAYS" },
                new StreetTypeViewModel { StreetTypeID = 137, Name = "RAMBLE" },
                new StreetTypeViewModel { StreetTypeID = 138, Name = "RAMP" },
                new StreetTypeViewModel { StreetTypeID = 139, Name = "REACH" },
                new StreetTypeViewModel { StreetTypeID = 140, Name = "RESERVE" },
                new StreetTypeViewModel { StreetTypeID = 141, Name = "REST" },
                new StreetTypeViewModel { StreetTypeID = 142, Name = "RETREAT" },
                new StreetTypeViewModel { StreetTypeID = 143, Name = "RETURN" },
                new StreetTypeViewModel { StreetTypeID = 144, Name = "RIDGE" },
                new StreetTypeViewModel { StreetTypeID = 145, Name = "RISE" },
                new StreetTypeViewModel { StreetTypeID = 146, Name = "RISING" },
                new StreetTypeViewModel { StreetTypeID = 147, Name = "RIVER" },
                new StreetTypeViewModel { StreetTypeID = 148, Name = "ROAD" },
                new StreetTypeViewModel { StreetTypeID = 149, Name = "ROADS" },
                new StreetTypeViewModel { StreetTypeID = 150, Name = "ROADWAY" },
                new StreetTypeViewModel { StreetTypeID = 151, Name = "ROUND" },
                new StreetTypeViewModel { StreetTypeID = 152, Name = "ROUTE" },
                new StreetTypeViewModel { StreetTypeID = 153, Name = "ROW" },
                new StreetTypeViewModel { StreetTypeID = 154, Name = "RUN" },
                new StreetTypeViewModel { StreetTypeID = 155, Name = "SERVICE WAY" },
                new StreetTypeViewModel { StreetTypeID = 156, Name = "SKYLINE" },
                new StreetTypeViewModel { StreetTypeID = 157, Name = "SLOPE" },
                new StreetTypeViewModel { StreetTypeID = 158, Name = "SPUR" },
                new StreetTypeViewModel { StreetTypeID = 159, Name = "SQUARE" },
                new StreetTypeViewModel { StreetTypeID = 160, Name = "STEPS" },
                new StreetTypeViewModel { StreetTypeID = 161, Name = "STRAIGHT" },
                new StreetTypeViewModel { StreetTypeID = 162, Name = "STRAIT" },
                new StreetTypeViewModel { StreetTypeID = 163, Name = "STREET" },
                new StreetTypeViewModel { StreetTypeID = 164, Name = "STRIP" },
                new StreetTypeViewModel { StreetTypeID = 165, Name = "SUBWAY" },
                new StreetTypeViewModel { StreetTypeID = 166, Name = "TARN" },
                new StreetTypeViewModel { StreetTypeID = 167, Name = "TERRACE" },
                new StreetTypeViewModel { StreetTypeID = 168, Name = "THROUGHWAY" },
                new StreetTypeViewModel { StreetTypeID = 169, Name = "TOP" },
                new StreetTypeViewModel { StreetTypeID = 170, Name = "TOR" },
                new StreetTypeViewModel { StreetTypeID = 171, Name = "TRACK" },
                new StreetTypeViewModel { StreetTypeID = 172, Name = "TRAIL" },
                new StreetTypeViewModel { StreetTypeID = 173, Name = "TRUNKWAY" },
                new StreetTypeViewModel { StreetTypeID = 174, Name = "TURN" },
                new StreetTypeViewModel { StreetTypeID = 175, Name = "TWIST" },
                new StreetTypeViewModel { StreetTypeID = 176, Name = "VALE" },
                new StreetTypeViewModel { StreetTypeID = 177, Name = "VALLEY" },
                new StreetTypeViewModel { StreetTypeID = 178, Name = "VIEW" },
                new StreetTypeViewModel { StreetTypeID = 179, Name = "VIEWS" },
                new StreetTypeViewModel { StreetTypeID = 180, Name = "VILLA" },
                new StreetTypeViewModel { StreetTypeID = 181, Name = "VISTA" },
                new StreetTypeViewModel { StreetTypeID = 182, Name = "WALK" },
                new StreetTypeViewModel { StreetTypeID = 183, Name = "WALKWAY" },
                new StreetTypeViewModel { StreetTypeID = 184, Name = "WATERS" },
                new StreetTypeViewModel { StreetTypeID = 185, Name = "WATERWAY" },
                new StreetTypeViewModel { StreetTypeID = 186, Name = "WAY" },
                new StreetTypeViewModel { StreetTypeID = 187, Name = "WHARF" },
                new StreetTypeViewModel { StreetTypeID = 188, Name = "WOODS" },
                new StreetTypeViewModel { StreetTypeID = 189, Name = "WYND" }
            };

            return streetTypes;
        }
        private IEnumerable<StateViewModel> GetAllStates()
        {
            IEnumerable<StateViewModel> states = new List<StateViewModel>
            {
                new StateViewModel { StateID = 1, Name = "NSW" },
                new StateViewModel { StateID = 2, Name = "ACT" },
                new StateViewModel { StateID = 3, Name = "QLD" },
                new StateViewModel { StateID = 4, Name = "TAS" },
                new StateViewModel { StateID = 5, Name = "VIC" },
                new StateViewModel { StateID = 6, Name = "SA" },
                new StateViewModel { StateID = 7, Name = "WA" },
                new StateViewModel { StateID = 8, Name = "NT" }
            };


            return states;
        }

        private IEnumerable<SecurityQuestionViewModel> GetAllSecurityQuestions()
        {
            IEnumerable<SecurityQuestionViewModel> securityQuestions = new List<SecurityQuestionViewModel>
            {
                new SecurityQuestionViewModel { SecurityQuestionID = 1, Question = "Father's city of birth" },
                new SecurityQuestionViewModel { SecurityQuestionID = 2, Question = "First pet's name" },
                new SecurityQuestionViewModel { SecurityQuestionID = 3, Question = "First school you attended" }
            };
            return securityQuestions;
        }

        public IActionResult CKeditorView()
        {
            return View();
        }
    }
}