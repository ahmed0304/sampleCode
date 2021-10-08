using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtihadForex.Data.Abstract;
using EtihadForex.Data.Models;
using EtihadForex.Utilities.Attributes;
using EtihadForex.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EtihadForex.Controllers
{

    [ServiceFilter(typeof(SessionExpireAttribute))]
    public class UserRolePrivilegesController : Controller
    {
        private readonly IUserRolePrivilegesRepository _userRolePrivilegesRepository;
        private readonly ILogger<UserRolePrivilegesController> _logger;

        public UserRolePrivilegesController(IUserRolePrivilegesRepository userRolePrivilegesRepository,
            ILogger<UserRolePrivilegesController> logger)
        {
            this._userRolePrivilegesRepository = userRolePrivilegesRepository;
            this._logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            UserRolePrivilegesViewModel userRolePrivilegesViewModel = new UserRolePrivilegesViewModel();
            var userRolePrivileges = (await _userRolePrivilegesRepository.GetAll()).FirstOrDefault();
            if (userRolePrivileges != null)
            {
                
                userRolePrivilegesViewModel.UserRolePrivilegeID = userRolePrivileges.UserRolePrivilegeID;
                // Reports
                userRolePrivilegesViewModel.ShowListOfOutgoingOrdersReport = userRolePrivileges.ShowListOfOutgoingOrdersReport;
                userRolePrivilegesViewModel.ShowListOfIncomingOrdersReport = userRolePrivileges.ShowListOfIncomingOrdersReport;
                userRolePrivilegesViewModel.ShowRefundTransactionsReport = userRolePrivileges.ShowRefundTransactionsReport;
                userRolePrivilegesViewModel.ShowCustomerComplienceReport = userRolePrivileges.ShowCustomerComplienceReport;
                userRolePrivilegesViewModel.ShowTransactionCommissionReport = userRolePrivileges.ShowTransactionCommissionReport;
                userRolePrivilegesViewModel.ShowCustomerDocumentExpirationReport = userRolePrivileges.ShowCustomerDocumentExpirationReport;
                userRolePrivilegesViewModel.ShowCurrencyTurnoverSummary = userRolePrivileges.ShowCurrencyTurnoverSummary;
                userRolePrivilegesViewModel.ShowInvoiceProfitReport = userRolePrivileges.ShowInvoiceProfitReport;
                userRolePrivilegesViewModel.ShowAccountStatement = userRolePrivileges.ShowAccountStatement;
                userRolePrivilegesViewModel.ShowOrderSummary = userRolePrivileges.ShowOrderSummary;
                userRolePrivilegesViewModel.ShowDailySummary = userRolePrivileges.ShowDailySummary;
                userRolePrivilegesViewModel.ShowAgentSummary = userRolePrivileges.ShowAgentSummary;
                // Main Menus
                userRolePrivilegesViewModel.ShowOrderMainMenu = userRolePrivileges.ShowOrderMainMenu;
                userRolePrivilegesViewModel.ShowCustomerMainMenu = userRolePrivileges.ShowCustomerMainMenu;
                userRolePrivilegesViewModel.ShowAgentInfoMainMenu = userRolePrivileges.ShowAgentInfoMainMenu;
                userRolePrivilegesViewModel.ShowAgentInfoMainMenu = userRolePrivileges.ShowAgentInfoMainMenu;
                userRolePrivilegesViewModel.ShowContactMainMenu = userRolePrivileges.ShowContactMainMenu;
                userRolePrivilegesViewModel.ShowReportsMainMenu = userRolePrivileges.ShowReportsMainMenu;
                userRolePrivilegesViewModel.ShowSupportMainMenu = userRolePrivileges.ShowSupportMainMenu;
                userRolePrivilegesViewModel.ShowSetupMainMenu = userRolePrivileges.ShowSetupMainMenu;

                // Order Menus
                userRolePrivilegesViewModel.ShowNewOrderMenu = userRolePrivileges.ShowNewOrderMenu;
                userRolePrivilegesViewModel.ShowViewOrderMenu = userRolePrivileges.ShowViewOrderMenu;
                userRolePrivilegesViewModel.ShowAllOrdersMenu = userRolePrivileges.ShowAllOrdersMenu;
                userRolePrivilegesViewModel.ShowSentOrderMenu = userRolePrivileges.ShowSentOrderMenu;
                userRolePrivilegesViewModel.ShowReceivedOrdersMenu = userRolePrivileges.ShowReceivedOrdersMenu;
                userRolePrivilegesViewModel.ShowSettleOrdersMenu = userRolePrivileges.ShowSettleOrdersMenu;
                userRolePrivilegesViewModel.ShowUploadOrdersMenu = userRolePrivileges.ShowUploadOrdersMenu;
                userRolePrivilegesViewModel.SystemConfiguration_OrderOptionConfiguration = userRolePrivileges.SystemConfiguration_OrderOptionConfiguration;
                //Customer Menu
                userRolePrivilegesViewModel.ShowViewCustomersMenu = userRolePrivileges.ShowViewCustomersMenu;
                userRolePrivilegesViewModel.ShowNewCustomersMenu = userRolePrivileges.ShowNewCustomersMenu;
                userRolePrivilegesViewModel.ShowGoupsMenu = userRolePrivileges.ShowGoupsMenu;
                userRolePrivilegesViewModel.ShowSendEmailMenu = userRolePrivileges.ShowSendEmailMenu;
                userRolePrivilegesViewModel.ShowMailHistoryMenu = userRolePrivileges.ShowMailHistoryMenu;
                userRolePrivilegesViewModel.ShowSanctionHistoryMenu = userRolePrivileges.ShowSanctionHistoryMenu;
                userRolePrivilegesViewModel.ShowAnnouncementsMenu = userRolePrivileges.ShowAnnouncementsMenu;

                //Company Menu
                userRolePrivilegesViewModel.ShowNewCompanyMenu = userRolePrivileges.ShowNewCompanyMenu;
                userRolePrivilegesViewModel.ShowViewCompanyMenu = userRolePrivileges.ShowViewCompanyMenu;
                userRolePrivilegesViewModel.ShowCompanyAnnouncementsMenu = userRolePrivileges.ShowCompanyAnnouncementsMenu;
                //Report Menu
                userRolePrivilegesViewModel.ShowAllReportsMenu = userRolePrivileges.ShowAllReportsMenu;
                userRolePrivilegesViewModel.ShowDownloadTransactionsMenu = userRolePrivileges.ShowDownloadTransactionsMenu;
                //Support Menu
                userRolePrivilegesViewModel.ShowSupportAnnouncementsMenu = userRolePrivileges.ShowSupportAnnouncementsMenu;
                userRolePrivilegesViewModel.ShowSMSServiceAccountMenu = userRolePrivileges.ShowSMSServiceAccountMenu;
                //Setup Menu
                userRolePrivilegesViewModel.ShowBakaalOptionsMenu = userRolePrivileges.ShowBakaalOptionsMenu;
                userRolePrivilegesViewModel.ShowCountryMenu = userRolePrivileges.ShowCountryMenu;
                userRolePrivilegesViewModel.ShowUserRolesMenu = userRolePrivileges.ShowUserRolesMenu;
                userRolePrivilegesViewModel.ShowUserRolePrivilegesMenu = userRolePrivileges.ShowUserRolePrivilegesMenu;
                userRolePrivilegesViewModel.ShowNewUserMenu = userRolePrivileges.ShowNewUserMenu;
                userRolePrivilegesViewModel.ShowViewUsersMenu = userRolePrivileges.ShowViewUsersMenu;
                userRolePrivilegesViewModel.ShowCurrencyMenu = userRolePrivileges.ShowCurrencyMenu;
                userRolePrivilegesViewModel.ShowSetMultiCurrencyMenu = userRolePrivileges.ShowSetMultiCurrencyMenu;
                userRolePrivilegesViewModel.ShowExchangeRatesMenu = userRolePrivileges.ShowExchangeRatesMenu;
                userRolePrivilegesViewModel.ShowAgentExchangeRatesMenu = userRolePrivileges.ShowAgentExchangeRatesMenu;
                userRolePrivilegesViewModel.ShowExchangeRateMarginSetupMenu = userRolePrivileges.ShowExchangeRateMarginSetupMenu;
                userRolePrivilegesViewModel.ShowExchangeRatesUpdateMenu = userRolePrivileges.ShowExchangeRatesUpdateMenu;
                userRolePrivilegesViewModel.ShowCommissionSlabSetupMenu = userRolePrivileges.ShowCommissionSlabSetupMenu;
                userRolePrivilegesViewModel.ShowAgentCommissionModeMenu = userRolePrivileges.ShowAgentCommissionModeMenu;
                userRolePrivilegesViewModel.ShowAgentPermissionSubAgentMenu = userRolePrivileges.ShowAgentPermissionSubAgentMenu;
                userRolePrivilegesViewModel.ShowTransactionLimitMenu = userRolePrivileges.ShowTransactionLimitMenu;
                userRolePrivilegesViewModel.ShowOneOffTransactionThresholdsMenu = userRolePrivileges.ShowOneOffTransactionThresholdsMenu;
                userRolePrivilegesViewModel.ShowLinkedTransactionThresholdsMenu = userRolePrivileges.ShowLinkedTransactionThresholdsMenu;
                userRolePrivilegesViewModel.ShowSystemConfigurationMenu = userRolePrivileges.ShowSystemConfigurationMenu;
                userRolePrivilegesViewModel.CompanyCreditLimits = userRolePrivileges.CompanyCreditLimits;
                userRolePrivilegesViewModel.ShowDatabaseBackupMenu = userRolePrivileges.ShowDatabaseBackupMenu;
                userRolePrivilegesViewModel.ShowCustomerValidationsMenu = userRolePrivileges.ShowCustomerValidationsMenu;
                userRolePrivilegesViewModel.ShowBeneficiaryValidationsMenu = userRolePrivileges.ShowBeneficiaryValidationsMenu;
                userRolePrivilegesViewModel.ShowGatewaysMenu = userRolePrivileges.ShowGatewaysMenu;
                userRolePrivilegesViewModel.ShowSetupBankCodesMenu = userRolePrivileges.ShowSetupBankCodesMenu;
                userRolePrivilegesViewModel.ShowSetupMCC_MNCcodesMenu = userRolePrivileges.ShowSetupMCC_MNCcodesMenu;
                userRolePrivilegesViewModel.ShowCompanySettlementMethodsMenu = userRolePrivileges.ShowCompanySettlementMethodsMenu;
                userRolePrivilegesViewModel.ShowAgentPayoutMethodsMenu = userRolePrivileges.ShowAgentPayoutMethodsMenu;
                userRolePrivilegesViewModel.ShowOnlineAgentTxnFeeMenu = userRolePrivileges.ShowOnlineAgentTxnFeeMenu;
                userRolePrivilegesViewModel.ShowPayoutLimitsMenu = userRolePrivileges.ShowPayoutLimitsMenu;
                userRolePrivilegesViewModel.TaxManu = userRolePrivileges.TaxManu;
                userRolePrivilegesViewModel.TaxConfigurationMenu = userRolePrivileges.TaxConfigurationMenu;
            }
            return View(userRolePrivilegesViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            CreateUserRolePrivilegesViewModel createModel = new CreateUserRolePrivilegesViewModel();
            var userRolePrivileges = (await _userRolePrivilegesRepository.GetAll()).FirstOrDefault();
            if (userRolePrivileges != null)
            {

                createModel.UserRolePrivilegeID = userRolePrivileges.UserRolePrivilegeID;
                // Reports
                createModel.ShowListOfOutgoingOrdersReport = userRolePrivileges.ShowListOfOutgoingOrdersReport;
                createModel.ShowListOfIncomingOrdersReport = userRolePrivileges.ShowListOfIncomingOrdersReport;
                createModel.ShowRefundTransactionsReport = userRolePrivileges.ShowRefundTransactionsReport;
                createModel.ShowCustomerComplienceReport = userRolePrivileges.ShowCustomerComplienceReport;
                createModel.ShowTransactionCommissionReport = userRolePrivileges.ShowTransactionCommissionReport;
                createModel.ShowCustomerDocumentExpirationReport = userRolePrivileges.ShowCustomerDocumentExpirationReport;
                createModel.ShowCurrencyTurnoverSummary = userRolePrivileges.ShowCurrencyTurnoverSummary;
                createModel.ShowInvoiceProfitReport = userRolePrivileges.ShowInvoiceProfitReport;
                createModel.ShowAccountStatement = userRolePrivileges.ShowAccountStatement;
                createModel.ShowOrderSummary = userRolePrivileges.ShowOrderSummary;
                createModel.ShowDailySummary = userRolePrivileges.ShowDailySummary;
                createModel.ShowAgentSummary = userRolePrivileges.ShowAgentSummary;
                // Main Menus
                createModel.ShowOrderMainMenu = userRolePrivileges.ShowOrderMainMenu;
                createModel.ShowCustomerMainMenu = userRolePrivileges.ShowCustomerMainMenu;
                createModel.ShowAgentInfoMainMenu = userRolePrivileges.ShowAgentInfoMainMenu;
                createModel.ShowCompanyMainMenu = userRolePrivileges.ShowCompanyMainMenu;
                createModel.ShowContactMainMenu = userRolePrivileges.ShowContactMainMenu;
                createModel.ShowReportsMainMenu = userRolePrivileges.ShowReportsMainMenu;
                createModel.ShowSupportMainMenu = userRolePrivileges.ShowSupportMainMenu;
                createModel.ShowSetupMainMenu = userRolePrivileges.ShowSetupMainMenu;

                // Order Menus
                createModel.ShowNewOrderMenu = userRolePrivileges.ShowNewOrderMenu;
                createModel.ShowViewOrderMenu = userRolePrivileges.ShowViewOrderMenu;
                createModel.ShowAllOrdersMenu = userRolePrivileges.ShowAllOrdersMenu;
                createModel.ShowSentOrderMenu = userRolePrivileges.ShowSentOrderMenu;
                createModel.ShowReceivedOrdersMenu = userRolePrivileges.ShowReceivedOrdersMenu;
                createModel.ShowSettleOrdersMenu = userRolePrivileges.ShowSettleOrdersMenu;
                createModel.ShowUploadOrdersMenu = userRolePrivileges.ShowUploadOrdersMenu;
                createModel.SystemConfiguration_OrderOptionConfiguration = userRolePrivileges.SystemConfiguration_OrderOptionConfiguration;
                //Customer Menu
                createModel.ShowViewCustomersMenu = userRolePrivileges.ShowViewCustomersMenu;
                createModel.ShowNewCustomersMenu = userRolePrivileges.ShowNewCustomersMenu;
                createModel.ShowGoupsMenu = userRolePrivileges.ShowGoupsMenu;
                createModel.ShowSendEmailMenu = userRolePrivileges.ShowSendEmailMenu;
                createModel.ShowMailHistoryMenu = userRolePrivileges.ShowMailHistoryMenu;
                createModel.ShowSanctionHistoryMenu = userRolePrivileges.ShowSanctionHistoryMenu;
                createModel.ShowAnnouncementsMenu = userRolePrivileges.ShowAnnouncementsMenu;

                //Company Menu
                createModel.ShowNewCompanyMenu = userRolePrivileges.ShowNewCompanyMenu;
                createModel.ShowViewCompanyMenu = userRolePrivileges.ShowViewCompanyMenu;
                createModel.ShowCompanyAnnouncementsMenu = userRolePrivileges.ShowCompanyAnnouncementsMenu;
                //Report Menu
                createModel.ShowAllReportsMenu = userRolePrivileges.ShowAllReportsMenu;
                createModel.ShowDownloadTransactionsMenu = userRolePrivileges.ShowDownloadTransactionsMenu;
                //Support Menu
                createModel.ShowSupportAnnouncementsMenu = userRolePrivileges.ShowSupportAnnouncementsMenu;
                createModel.ShowSMSServiceAccountMenu = userRolePrivileges.ShowSMSServiceAccountMenu;
                //Setup Menu
                createModel.ShowBakaalOptionsMenu = userRolePrivileges.ShowBakaalOptionsMenu;
                createModel.ShowCountryMenu = userRolePrivileges.ShowCountryMenu;
                createModel.ShowUserRolesMenu = userRolePrivileges.ShowUserRolesMenu;
                createModel.ShowUserRolePrivilegesMenu = userRolePrivileges.ShowUserRolePrivilegesMenu;
                createModel.ShowNewUserMenu = userRolePrivileges.ShowNewUserMenu;
                createModel.ShowViewUsersMenu = userRolePrivileges.ShowViewUsersMenu;
                createModel.ShowCurrencyMenu = userRolePrivileges.ShowCurrencyMenu;
                createModel.ShowSetMultiCurrencyMenu = userRolePrivileges.ShowSetMultiCurrencyMenu;
                createModel.ShowExchangeRatesMenu = userRolePrivileges.ShowExchangeRatesMenu;
                createModel.ShowAgentExchangeRatesMenu = userRolePrivileges.ShowAgentExchangeRatesMenu;
                createModel.ShowExchangeRateMarginSetupMenu = userRolePrivileges.ShowExchangeRateMarginSetupMenu;
                createModel.ShowExchangeRatesUpdateMenu = userRolePrivileges.ShowExchangeRatesUpdateMenu;
                createModel.ShowCommissionSlabSetupMenu = userRolePrivileges.ShowCommissionSlabSetupMenu;
                createModel.ShowAgentCommissionModeMenu = userRolePrivileges.ShowAgentCommissionModeMenu;
                createModel.ShowAgentPermissionSubAgentMenu = userRolePrivileges.ShowAgentPermissionSubAgentMenu;
                createModel.ShowTransactionLimitMenu = userRolePrivileges.ShowTransactionLimitMenu;
                createModel.ShowOneOffTransactionThresholdsMenu = userRolePrivileges.ShowOneOffTransactionThresholdsMenu;
                createModel.ShowLinkedTransactionThresholdsMenu = userRolePrivileges.ShowLinkedTransactionThresholdsMenu;
                createModel.ShowSystemConfigurationMenu = userRolePrivileges.ShowSystemConfigurationMenu;
                createModel.CompanyCreditLimits = userRolePrivileges.CompanyCreditLimits;
                createModel.ShowDatabaseBackupMenu = userRolePrivileges.ShowDatabaseBackupMenu;
                createModel.ShowCustomerValidationsMenu = userRolePrivileges.ShowCustomerValidationsMenu;
                createModel.ShowBeneficiaryValidationsMenu = userRolePrivileges.ShowBeneficiaryValidationsMenu;
                createModel.ShowGatewaysMenu = userRolePrivileges.ShowGatewaysMenu;
                createModel.ShowSetupBankCodesMenu = userRolePrivileges.ShowSetupBankCodesMenu;
                createModel.ShowSetupMCC_MNCcodesMenu = userRolePrivileges.ShowSetupMCC_MNCcodesMenu;
                createModel.ShowCompanySettlementMethodsMenu = userRolePrivileges.ShowCompanySettlementMethodsMenu;
                createModel.ShowAgentPayoutMethodsMenu = userRolePrivileges.ShowAgentPayoutMethodsMenu;
                createModel.ShowOnlineAgentTxnFeeMenu = userRolePrivileges.ShowOnlineAgentTxnFeeMenu;
                createModel.ShowPayoutLimitsMenu = userRolePrivileges.ShowPayoutLimitsMenu;
                createModel.TaxManu = userRolePrivileges.TaxManu;
                createModel.TaxConfigurationMenu = userRolePrivileges.TaxConfigurationMenu;
            }
            return View(createModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRolePrivilegesViewModel createModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    UserRolePrivileges createUserRolePrivileges = new UserRolePrivileges();
                    // Reports
                    createUserRolePrivileges.ShowListOfOutgoingOrdersReport = createModel.ShowListOfOutgoingOrdersReport;
                    createUserRolePrivileges.ShowListOfIncomingOrdersReport = createModel.ShowListOfIncomingOrdersReport;
                    createUserRolePrivileges.ShowRefundTransactionsReport = createModel.ShowRefundTransactionsReport;
                    createUserRolePrivileges.ShowCustomerComplienceReport = createModel.ShowCustomerComplienceReport;
                    createUserRolePrivileges.ShowTransactionCommissionReport = createModel.ShowTransactionCommissionReport;
                    createUserRolePrivileges.ShowCustomerDocumentExpirationReport = createModel.ShowCustomerDocumentExpirationReport;
                    createUserRolePrivileges.ShowCurrencyTurnoverSummary = createModel.ShowCurrencyTurnoverSummary;
                    createUserRolePrivileges.ShowInvoiceProfitReport = createModel.ShowInvoiceProfitReport;
                    createUserRolePrivileges.ShowAccountStatement = createModel.ShowAccountStatement;
                    createUserRolePrivileges.ShowOrderSummary = createModel.ShowOrderSummary;
                    createUserRolePrivileges.ShowDailySummary = createModel.ShowDailySummary;
                    createUserRolePrivileges.ShowAgentSummary = createModel.ShowAgentSummary;
                    // Main Menus
                    createUserRolePrivileges.ShowOrderMainMenu = createModel.ShowOrderMainMenu;
                    createUserRolePrivileges.ShowCustomerMainMenu = createModel.ShowCustomerMainMenu;
                    createUserRolePrivileges.ShowAgentInfoMainMenu = createModel.ShowAgentInfoMainMenu;
                    createUserRolePrivileges.ShowCompanyMainMenu = createModel.ShowCompanyMainMenu;
                    createUserRolePrivileges.ShowContactMainMenu = createModel.ShowContactMainMenu;
                    createUserRolePrivileges.ShowReportsMainMenu = createModel.ShowReportsMainMenu;
                    createUserRolePrivileges.ShowSupportMainMenu = createModel.ShowSupportMainMenu;
                    createUserRolePrivileges.ShowSetupMainMenu = createModel.ShowSetupMainMenu;

                    // Order Menus
                    createUserRolePrivileges.ShowNewOrderMenu = createModel.ShowNewOrderMenu;
                    createUserRolePrivileges.ShowViewOrderMenu = createModel.ShowViewOrderMenu;
                    createUserRolePrivileges.ShowAllOrdersMenu = createModel.ShowAllOrdersMenu;
                    createUserRolePrivileges.ShowSentOrderMenu = createModel.ShowSentOrderMenu;
                    createUserRolePrivileges.ShowReceivedOrdersMenu = createModel.ShowReceivedOrdersMenu;
                    createUserRolePrivileges.ShowSettleOrdersMenu = createModel.ShowSettleOrdersMenu;
                    createUserRolePrivileges.ShowUploadOrdersMenu = createModel.ShowUploadOrdersMenu;
                    createUserRolePrivileges.SystemConfiguration_OrderOptionConfiguration = createModel.SystemConfiguration_OrderOptionConfiguration;
                    //Customer Menu
                    createUserRolePrivileges.ShowViewCustomersMenu = createModel.ShowViewCustomersMenu;
                    createUserRolePrivileges.ShowNewCustomersMenu = createModel.ShowNewCustomersMenu;
                    createUserRolePrivileges.ShowGoupsMenu = createModel.ShowGoupsMenu;
                    createUserRolePrivileges.ShowSendEmailMenu = createModel.ShowSendEmailMenu;
                    createUserRolePrivileges.ShowMailHistoryMenu = createModel.ShowMailHistoryMenu;
                    createUserRolePrivileges.ShowSanctionHistoryMenu = createModel.ShowSanctionHistoryMenu;
                    createUserRolePrivileges.ShowAnnouncementsMenu = createModel.ShowAnnouncementsMenu;

                    //Company Menu
                    createUserRolePrivileges.ShowNewCompanyMenu = createModel.ShowNewCompanyMenu;
                    createUserRolePrivileges.ShowViewCompanyMenu = createModel.ShowViewCompanyMenu;
                    createUserRolePrivileges.ShowCompanyAnnouncementsMenu = createModel.ShowCompanyAnnouncementsMenu;
                    //Report Menu
                    createUserRolePrivileges.ShowAllReportsMenu = createModel.ShowAllReportsMenu;
                    createUserRolePrivileges.ShowDownloadTransactionsMenu = createModel.ShowDownloadTransactionsMenu;
                    //Support Menu
                    createUserRolePrivileges.ShowSupportAnnouncementsMenu = createModel.ShowSupportAnnouncementsMenu;
                    createUserRolePrivileges.ShowSMSServiceAccountMenu = createModel.ShowSMSServiceAccountMenu;
                    //Setup Menu
                    createUserRolePrivileges.ShowBakaalOptionsMenu = createModel.ShowBakaalOptionsMenu;
                    createUserRolePrivileges.ShowCountryMenu = createModel.ShowCountryMenu;
                    createUserRolePrivileges.ShowUserRolesMenu = createModel.ShowUserRolesMenu;
                    createUserRolePrivileges.ShowUserRolePrivilegesMenu = createModel.ShowUserRolePrivilegesMenu;
                    createUserRolePrivileges.ShowNewUserMenu = createModel.ShowNewUserMenu;
                    createUserRolePrivileges.ShowViewUsersMenu = createModel.ShowViewUsersMenu;
                    createUserRolePrivileges.ShowCurrencyMenu = createModel.ShowCurrencyMenu;
                    createUserRolePrivileges.ShowSetMultiCurrencyMenu = createModel.ShowSetMultiCurrencyMenu;
                    createUserRolePrivileges.ShowExchangeRatesMenu = createModel.ShowExchangeRatesMenu;
                    createUserRolePrivileges.ShowAgentExchangeRatesMenu = createModel.ShowAgentExchangeRatesMenu;
                    createUserRolePrivileges.ShowExchangeRateMarginSetupMenu = createModel.ShowExchangeRateMarginSetupMenu;
                    createUserRolePrivileges.ShowExchangeRatesUpdateMenu = createModel.ShowExchangeRatesUpdateMenu;
                    createUserRolePrivileges.ShowCommissionSlabSetupMenu = createModel.ShowCommissionSlabSetupMenu;
                    createUserRolePrivileges.ShowAgentCommissionModeMenu = createModel.ShowAgentCommissionModeMenu;
                    createUserRolePrivileges.ShowAgentPermissionSubAgentMenu = createModel.ShowAgentPermissionSubAgentMenu;
                    createUserRolePrivileges.ShowTransactionLimitMenu = createModel.ShowTransactionLimitMenu;
                    createUserRolePrivileges.ShowOneOffTransactionThresholdsMenu = createModel.ShowOneOffTransactionThresholdsMenu;
                    createUserRolePrivileges.ShowLinkedTransactionThresholdsMenu = createModel.ShowLinkedTransactionThresholdsMenu;
                    createUserRolePrivileges.ShowSystemConfigurationMenu = createModel.ShowSystemConfigurationMenu;
                    createUserRolePrivileges.CompanyCreditLimits = createModel.CompanyCreditLimits;
                    createUserRolePrivileges.ShowDatabaseBackupMenu = createModel.ShowDatabaseBackupMenu;
                    createUserRolePrivileges.ShowCustomerValidationsMenu = createModel.ShowCustomerValidationsMenu;
                    createUserRolePrivileges.ShowBeneficiaryValidationsMenu = createModel.ShowBeneficiaryValidationsMenu;
                    createUserRolePrivileges.ShowGatewaysMenu = createModel.ShowGatewaysMenu;
                    createUserRolePrivileges.ShowSetupBankCodesMenu = createModel.ShowSetupBankCodesMenu;
                    createUserRolePrivileges.ShowSetupMCC_MNCcodesMenu = createModel.ShowSetupMCC_MNCcodesMenu;
                    createUserRolePrivileges.ShowCompanySettlementMethodsMenu = createModel.ShowCompanySettlementMethodsMenu;
                    createUserRolePrivileges.ShowAgentPayoutMethodsMenu = createModel.ShowAgentPayoutMethodsMenu;
                    createUserRolePrivileges.ShowOnlineAgentTxnFeeMenu = createModel.ShowOnlineAgentTxnFeeMenu;
                    createUserRolePrivileges.ShowPayoutLimitsMenu = createModel.ShowPayoutLimitsMenu;
                    createUserRolePrivileges.TaxManu = createModel.TaxManu;
                    createUserRolePrivileges.TaxConfigurationMenu = createModel.TaxConfigurationMenu;
                    if(createModel.UserRolePrivilegeID > 0)
                    {
                        createUserRolePrivileges.UserRolePrivilegeID = createModel.UserRolePrivilegeID;
                        var userRolePrivilegesUpdated = await _userRolePrivilegesRepository.Update(createUserRolePrivileges);
                        if (userRolePrivilegesUpdated != null)
                        {
                            ViewBag.Success = "Privileges saved successfully";
                            //return RedirectToAction(nameof(Create));
                        }
                        else
                        {
                            ViewBag.Success = "Privileges not saved";
                        }
                    }
                    else
                    {
                        var userRolePrivilegesCreated = await _userRolePrivilegesRepository.Add(createUserRolePrivileges);
                        if (userRolePrivilegesCreated != null)
                        {
                            ViewBag.Success = "Privileges saved successfully";
                            //return RedirectToAction(nameof(Create));
                        }
                        else
                        {
                            ViewBag.Success = "Privileges not saved";
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    ModelState.AddModelError("", "An exeption has been occured, please contact system administrtor");
                }
            }

            return View(createModel);
        }
    }
}