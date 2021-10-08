using EtihadForex.Areas.Admin.ViewModel;
using EtihadForex.Data.Abstract;
using EtihadForex.Data.Models;
using EtihadForex.Email;
using EtihadForex.SiteSystem.SendGrid;
using EtihadForex.Utilities.Attributes;
using EtihadForex.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtihadForex.Controllers
{
    [ServiceFilter(typeof(SessionExpireAttribute))]
    public class EmailController : Controller
    {
        private readonly ICustomerEmailHistoryRepository _customerEmailHistoryRepository;
        private readonly IEmailSender _emailSender;

        private readonly ICountryRepository _countryRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserPersonalDetailRepository _userPersonalDetailRepository;
        private readonly IUserDocumentDetailRepository _userDocumentDetailRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRecipientRepository _recipientRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly ICustomerCommentsRepository _customerCommentsRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICustomerGroupRepository _customerGroupRepository;
        private readonly IGroupCustomersRepository _groupCustomersRepository;
        private readonly ICustomerAnnouncementRepository _customerAnnouncementRepository;
        private readonly ILogger<EmailController> _logger;
        private readonly IConfiguration _config;

        bool isSuccess = false;
        bool isWarning = false;

        public EmailController(ICustomerEmailHistoryRepository customerEmailHistoryRepository, IEmailSender emailSender,
            ICountryRepository countryRepository,
             ICompanyRepository companyRepository, IHttpContextAccessor httpContextAccessor, IUserPersonalDetailRepository userPersonalDetailRepository,
             IUserDocumentDetailRepository userDocumentDetailRepository, IDocumentRepository documentRepository,
             UserManager<ApplicationUser> userManager,
             IRecipientRepository recipientRepository, ICurrencyRepository currencyRepository,
             ICustomerCommentsRepository customerCommentsRepository, ITransactionRepository transactionRepository,
             ICustomerGroupRepository customerGroupRepository,
             IGroupCustomersRepository groupCustomersRepository, ICustomerAnnouncementRepository customerAnnouncementRepository,
            ILogger<EmailController> logger, IConfiguration configuration)
        {
            this._customerEmailHistoryRepository = customerEmailHistoryRepository;
            this._emailSender = emailSender;
            this._logger = logger;


            this._countryRepository = countryRepository;
            this._companyRepository = companyRepository;
            this._httpContextAccessor = httpContextAccessor;
            this._userPersonalDetailRepository = userPersonalDetailRepository;
            this._userDocumentDetailRepository = userDocumentDetailRepository;
            this._documentRepository = documentRepository;
            this._userManager = userManager;
            this._emailSender = emailSender;
            this._recipientRepository = recipientRepository;
            this._currencyRepository = currencyRepository;
            this._customerCommentsRepository = customerCommentsRepository;
            this._transactionRepository = transactionRepository;
            this._customerEmailHistoryRepository = customerEmailHistoryRepository;
            this._customerGroupRepository = customerGroupRepository;
            this._groupCustomersRepository = groupCustomersRepository;
            this._customerAnnouncementRepository = customerAnnouncementRepository;
            this._logger = logger;
            this._config = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SendMail()
        {
            GroupsViewModel groupsViewModel = new GroupsViewModel();
            try
            {
                var customerGroups = (await _customerGroupRepository.GetAll());
                if (customerGroups != null)
                {
                    groupsViewModel.CustomerGroupViewModelList = customerGroups.Select(y => new CustomerGroupViewModel
                    {
                        CustomerGroupID = y.CustomerGroupID,
                        Name = y.Name
                    });
                }

                var users = (await _userPersonalDetailRepository.GetAll());
                if (users != null)
                {
                    groupsViewModel.UserPersonalDetails = users.Select(y => new UserPersonalDetailViewModel
                    {
                        UserPersonalDetailID = y.UserPersonalDetailID,
                        ConcatName = $"{y.FirstName} {y.LastName} {y.MiddleName}"
                    });
                }

                var groupCustomers = (await _groupCustomersRepository.GetAll());

                if (groupCustomers != null)
                {
                    groupsViewModel.GroupCustomersList = groupCustomers.Select(x => new GroupCustomersViewModel
                    {
                        GroupCustomersID = x.GroupCustomersID,
                        CustomerGroup = new CustomerGroupViewModel
                        {
                            CustomerGroupID = x.CustomerGroup.CustomerGroupID,
                            Name = x.CustomerGroup.Name
                        },
                        UserPersonalDetail = new UserPersonalDetailViewModel
                        {
                            ConcatName = $"{x.UserPersonalDetail.FirstName} {x.UserPersonalDetail.MiddleName} {x.UserPersonalDetail.LastName}",
                            UserPersonalDetailID = x.UserPersonalDetailID
                        }
                    });
                }
            }
            catch (Exception ex)
            {
            }
            return View(groupsViewModel);
        }



        [HttpPost]
        public async Task<JsonResult> SendMailToEntity(int GroupID, int CustomerID, string selectedEntity, string Note)
        {
            string message = string.Empty;
            IEnumerable<GroupCustomersViewModel> groupCustomersList = new List<GroupCustomersViewModel>();
            try
            {

                CustomerEmailHistory newCustomerEmailHistory = new CustomerEmailHistory();
                newCustomerEmailHistory.From = "forexetihad@gmail.com";
                newCustomerEmailHistory.Body = Note;
                newCustomerEmailHistory.Status = true;
                newCustomerEmailHistory.CreatedDate = DateTime.Now;
                newCustomerEmailHistory.UpdatedDate = DateTime.Now;

                if (selectedEntity != null && selectedEntity.ToLower() == "selectedgroup")
                {
                    var customerGroup = await _customerGroupRepository.Get(GroupID);
                    if (customerGroup != null)
                    {
                        var groupCustomers = (await _groupCustomersRepository.GetAll()).Where(x => x.CustomerGroupID == customerGroup.CustomerGroupID);

                        if (groupCustomers != null)
                        {
                            newCustomerEmailHistory.To = "customer@group.com";
                            newCustomerEmailHistory.Subject = "Email to group";
                            newCustomerEmailHistory.CustomerGroupID = customerGroup.CustomerGroupID;

                            foreach (var item in groupCustomers)
                            {
                                var userFound = await _userPersonalDetailRepository.Get(item.UserPersonalDetailID);
                                var emailList = new List<string> { userFound.Email };
                                //var emailResponse = await _emailSender.SendEmailAsync(new List<string>(emailList), "Email to group", Note);
                                new EmailHelper(_config).SendEmailBySMTP(new List<string>(emailList), "Email to group", Note);

                                //if (emailResponse?.StatusCode.ToString().ToLower() == "accepted")
                                //{
                                //    message = $"Email sent to selected group successfully";
                                //    isSuccess = true;
                                //}
                                //else
                                //{
                                //    message = $"Email not sent";
                                //    isSuccess = false;
                                //}
                            }
                        }

                    }
                }
                else if (selectedEntity != null && selectedEntity.ToLower() == "selectedcustomer")
                {
                    var userFound = await _userPersonalDetailRepository.Get(CustomerID);
                    var emailList = new List<string> { userFound.Email };
                    // var emailResponse = await _emailSender.SendEmailAsync(new List<string>(emailList), "Email to customer", Note);
                    new EmailHelper(_config).SendEmailBySMTP(new List<string>(emailList), "Email to customer", Note);
                    newCustomerEmailHistory.To = emailList.ToString();
                    newCustomerEmailHistory.Subject = "Email to customer";
                    newCustomerEmailHistory.UserPersonalDetailID = userFound.UserPersonalDetailID;
                    message = $"Email sent to {userFound.FirstName} {userFound.MiddleName} {userFound.LastName} successfully";
                    isSuccess = true;
                    //if (emailResponse?.StatusCode.ToString().ToLower() == "accepted")
                    //{
                    //    newCustomerEmailHistory.To = emailList.ToString();
                    //    newCustomerEmailHistory.Subject = "Email to customer";
                    //    newCustomerEmailHistory.UserPersonalDetailID = userFound.UserPersonalDetailID;
                    //    message = $"Email sent to {userFound.FirstName} {userFound.MiddleName} {userFound.LastName} successfully";
                    //    isSuccess = true;
                    //}
                    //else
                    //{
                    //    message = $"Email not sent";
                    //    isSuccess = false;

                    //}
                }

                var recordAdded = await _customerEmailHistoryRepository.Add(newCustomerEmailHistory);
                if (recordAdded != null)
                {
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = "An exception has been occured, please contact system administrator";
                this._logger.LogError(ex.Message);
            }

            var jsonResult = new
            {
                isSuccess,
                isWarning,
                message,
            };
            return Json(jsonResult);
        }


        [HttpPost]
        public async Task<JsonResult> SendEmail(CustomerEmailHistoryCreateViewModel createModel)
        {

            bool isSuccess = false;
            bool isWarning = false;
            string message = string.Empty;
            IEnumerable<CustomerEmailHistoryViewModel> customerEmailHistoryList = new List<CustomerEmailHistoryViewModel>();
            try
            {
                var emailList = new List<string> { createModel.To };
                //var emailResponse = await _emailSender.SendEmailAsync(new List<string>(emailList), createModel.Subject, createModel.Body);
                new EmailHelper(_config).SendEmailBySMTP(new List<string>(emailList), createModel.Subject, createModel.Body);
                //if (emailResponse?.StatusCode.ToString().ToLower() == "accepted")
                //{
                if (ModelState.IsValid)
                {
                    CustomerEmailHistory newCustomerEmailHistory = new CustomerEmailHistory();
                    newCustomerEmailHistory.To = createModel.To;
                    newCustomerEmailHistory.From = createModel.To;
                    newCustomerEmailHistory.Subject = createModel.Subject;
                    newCustomerEmailHistory.Body = createModel.Body;
                    newCustomerEmailHistory.Status = true;
                    newCustomerEmailHistory.UserPersonalDetailID = createModel.UserPersonalDetailID;
                    newCustomerEmailHistory.CreatedDate = DateTime.Now;
                    newCustomerEmailHistory.UpdatedDate = DateTime.Now;
                    var recordAdded = await _customerEmailHistoryRepository.Add(newCustomerEmailHistory);
                    if (recordAdded != null)
                    {
                        isSuccess = true;
                        message = "Email sent successfully, Please click on the email history tab to view all emails";
                        //customerEmailHistoryList = (await _customerEmailHistoryRepository.GetAll())
                        //    .Where(x => x.UserPersonalDetailID == createModel.UserPersonalDetailID).Select(y => new CustomerEmailHistoryViewModel
                        //    {

                        //    });

                        var emailList_1 = (await _customerEmailHistoryRepository.GetAll()).Where(x => x.UserPersonalDetailID == createModel.UserPersonalDetailID).OrderByDescending(d => d.CreatedDate);
                        IList<CustomerEmailHistoryViewModel> emailHistoryList = new List<CustomerEmailHistoryViewModel>();
                        if (emailList_1 != null)
                            foreach (var item in emailList_1)
                            {
                                CustomerEmailHistoryViewModel customerEmailHistoryViewModel = new CustomerEmailHistoryViewModel();
                                customerEmailHistoryViewModel.CustomerEmailHistoryID = item.CustomerEmailHistoryID;

                                customerEmailHistoryViewModel.CustomerGroupID = item.CustomerGroupID;
                                if (item.CustomerGroup != null)
                                    customerEmailHistoryViewModel.CustomerGroup = new CustomerGroupViewModel
                                    {
                                        CustomerGroupID = item.CustomerGroup.CustomerGroupID,
                                        Name = item.CustomerGroup.Name
                                    };
                                customerEmailHistoryViewModel.UserPersonalDetailID = item.UserPersonalDetailID;
                                if (item.UserPersonalDetail != null)
                                    customerEmailHistoryViewModel.UserPersonalDetail = new UserPersonalDetailViewModel
                                    {
                                        UserPersonalDetailID = item.UserPersonalDetail.UserPersonalDetailID,
                                        ConcatName = $"{item.UserPersonalDetail.FirstName} {item.UserPersonalDetail.MiddleName} {item.UserPersonalDetail.LastName}"
                                    };
                                customerEmailHistoryViewModel.Subject = item.Subject;
                                customerEmailHistoryViewModel.Status = item.Status;
                                customerEmailHistoryViewModel.Body = item.Body.Replace("\n", " ").Replace("\r\n", "");
                                emailHistoryList.Add(customerEmailHistoryViewModel);
                            }

                        customerEmailHistoryList = emailHistoryList;
                    }
                    else
                    {
                        isSuccess = false;
                        message = "Email not sent";
                    }
                }
                else
                {
                    isWarning = true;
                    message = "Please fill up all the required fields";
                }
                //}
                //else
                //{
                //    isSuccess = true;
                //    message = "Email not send, please try again";
                //}

            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = "An exception has been occured please contact system administrator";
                _logger.LogError(ex.Message);
            }


            var jsonResult = new
            {
                isWarning,
                isSuccess,
                message,
                customerEmailHistoryList
            };
            return Json(jsonResult);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerEmailHistory()
        {
            var emailList = (await _customerEmailHistoryRepository.GetAll()).OrderByDescending(d => d.CreatedDate);
            IList<CustomerEmailHistoryViewModel> emailHistoryList = new List<CustomerEmailHistoryViewModel>();
            foreach (var item in emailList)
            {
                CustomerEmailHistoryViewModel customerEmailHistoryViewModel = new CustomerEmailHistoryViewModel();
                customerEmailHistoryViewModel.CustomerEmailHistoryID = item.CustomerEmailHistoryID;

                customerEmailHistoryViewModel.CustomerGroupID = item.CustomerGroupID;
                if (item.CustomerGroup != null)
                    customerEmailHistoryViewModel.CustomerGroup = new CustomerGroupViewModel
                    {
                        CustomerGroupID = item.CustomerGroup.CustomerGroupID,
                        Name = item.CustomerGroup.Name
                    };
                customerEmailHistoryViewModel.UserPersonalDetailID = item.UserPersonalDetailID;
                if (item.UserPersonalDetail != null)
                    customerEmailHistoryViewModel.UserPersonalDetail = new UserPersonalDetailViewModel
                    {
                        UserPersonalDetailID = item.UserPersonalDetail.UserPersonalDetailID,
                        ConcatName = $"{item.UserPersonalDetail.FirstName} {item.UserPersonalDetail.MiddleName} {item.UserPersonalDetail.LastName}"
                    };
                customerEmailHistoryViewModel.Subject = item.Subject;
                customerEmailHistoryViewModel.Status = item.Status;
                customerEmailHistoryViewModel.Body = item.Body.Replace("\n", " ").Replace("\r\n", "");//System.Text.RegularExpressions.Regex.Replace(item.Body, "[\\r\\n]+", System.Environment.NewLine, System.Text.RegularExpressions.RegexOptions.Multiline);
                emailHistoryList.Add(customerEmailHistoryViewModel);
            }
            return View(emailHistoryList);
        }

        [HttpPost]
        public async Task<JsonResult> DeleteCustomerEmail(int customerEmailHistoryID)
        {

            bool isSuccess = false;
            bool isWarning = false;
            string message = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {

                    var recordDeleted = await _customerEmailHistoryRepository.Delete(customerEmailHistoryID);
                    if (recordDeleted != null)
                    {
                        isSuccess = true;
                        message = "Email deleted successfully";
                    }
                    else
                    {
                        isSuccess = false;
                        message = "Email not deleted";
                    }
                }
                else
                {
                    isWarning = true;
                    message = "Email history not found";
                }

            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = "An exception has been occured please contact system administrator";
                _logger.LogError(ex.Message);
            }


            var jsonResult = new { isWarning, isSuccess, message };
            return Json(jsonResult);
        }

        [HttpPost]
        public async Task<JsonResult> ResendCustomerEmail(int customerEmailHistoryID)
        {

            bool isSuccess = false;
            bool isWarning = false;
            string message = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    var emailFound = await _customerEmailHistoryRepository.Get(customerEmailHistoryID);
                    if (emailFound != null)
                    {
                        var emailList = new List<string> { emailFound.To };
                        //var emailResponse = await _emailSender.SendEmailAsync(new List<string>(emailList), emailFound.Subject, emailFound.Body);
                        new EmailHelper(_config).SendEmailBySMTP(new List<string>(emailList), emailFound.Subject, emailFound.Body);

                        //if (emailResponse?.StatusCode.ToString().ToLower() == "accepted")
                        //{
                        CustomerEmailHistory newCustomerEmailHistory = new CustomerEmailHistory();
                        newCustomerEmailHistory.To = emailFound.To;
                        newCustomerEmailHistory.From = emailFound.To;
                        newCustomerEmailHistory.Subject = emailFound.Subject;
                        newCustomerEmailHistory.Body = emailFound.Body;
                        newCustomerEmailHistory.Status = emailFound.Status;
                        newCustomerEmailHistory.UserPersonalDetailID = emailFound.UserPersonalDetailID;
                        newCustomerEmailHistory.CreatedDate = DateTime.Now;
                        newCustomerEmailHistory.UpdatedDate = DateTime.Now;
                        var recordAdded = await _customerEmailHistoryRepository.Add(newCustomerEmailHistory);
                        if (recordAdded != null)
                        {
                            isSuccess = true;
                            message = "Email resent successfully, Please click on the email history tab to view all emails";
                        }
                        else
                        {
                            isSuccess = false;
                            message = "Email doesn't resent";
                        }
                        //}
                        //else
                        //{
                        //    isWarning = true;
                        //    message = "Email doesn't resent, please confirm your email address";
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                message = "An exception has been occured please contact system administrator";
                _logger.LogError(ex.Message);
            }
            var jsonResult = new { isWarning, isSuccess, message };
            return Json(jsonResult);
        }
    }
}