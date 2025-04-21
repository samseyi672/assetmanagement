using EntityProject.repositories;
using EntityProject.repositoriesimpl;
using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Retailbanking.BL.IServices;
using Retailbanking.BL.Services;
using Retailbanking.Common.CustomObj;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HistoricalPerformanceEntity = EntityProject.entities.HistoricalPerformance;
using TMMFDetailEntity = EntityProject.entities.TMMFDetail;

namespace assetmanagement.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IGenericAssetCapitalInsuranceCustomerService _customerServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly ITMMF tMMF;
        private readonly FolderPaths _folderPaths;
        private readonly IHistoricalPerformance historicalPerformance;

        public PortfolioController(IOptions<FolderPaths> folderPaths, IPortfolioService portfolioService, IGenericAssetCapitalInsuranceCustomerService customerServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, ITMMF tMMF, IHistoricalPerformance historicalPerformance)
        {
            _portfolioService = portfolioService;
            _customerServ = customerServ;
            _logger = logger;
            this.tMMF = tMMF;
            _folderPaths = folderPaths.Value;
            this.historicalPerformance = historicalPerformance;
        }



        /// <summary>
        ///Description: The endpoint allows you to fetch each portfolio and more details Request  
        ///that customer can choose to invest or subscribe to
        /// </summary>
        [HttpGet("GetFullProductDetails/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetFullProductDetails(string Session, string UserType)
        {
            _logger.LogInformation("GetFullProductDetails " + Session);
            GenericResponse2 genericResponse2 = await _portfolioService.GetFullProductDetails(Session, UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            if (genericResponse2.Success)
            {
                var tmf = tMMF.GetAllTMMF().FirstOrDefault();
                var hist = historicalPerformance.GetAllHistoricalPerformance().ToList();
                tmf.HistoricalPerformance = JsonConvert.SerializeObject(hist);
                string host = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.PathBase;
                string picpath = host + "/" + "api/Portfolio/BrowserView/"+"trustbancInvestmentIcon";
                object[] i = {new {tmf, historicalperformance = hist,logo=picpath }};
                var ApiResponseDto = (ApiResponseDto)genericResponse2.data;
                genericResponse2.data = new
                {
                    data=ApiResponseDto,
                    portfoliodetails = i
                };
                return genericResponse2;
            }
            return genericResponse2;
        }

        /// <summary>
        ///Description: The endpoint allows you to fetch each portfolio and more details Request      
        /// </summary>
        [HttpGet("GetPortfolioBalance/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetPortfolioBalance(string UserName, string Session, string UserType)
        {
            _logger.LogInformation("GetPortfolioBalance " + UserName);
            GenericResponse2 genericResponse2 = await _portfolioService.GetPortfolioBalance(UserName,Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///Description: The endpoint allows you to fetch transaction histories of all fixed deposit       
        /// </summary>
        [HttpGet("GetfixedDepositPortfolioHistories/{UserName}/{Session}/{UserType}/{startDate}/{endDate}")]
        public async Task<GenericResponse2> GetfixedDepositPortfolioHistories(int portfolioId, string UserName,string Session,string UserType, string startDate, string endDate,[FromQuery] int skip,[FromQuery] int pageSize)
        {
            _logger.LogInformation("GetfixedDepositPortfolioHistories " + UserName);
            GenericResponse2 genericResponse2 = await _portfolioService.GetfixedDepositPortfolioHistories(portfolioId, UserName,Session,UserType, startDate, endDate, skip, pageSize);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///Description: The endpoint allows you to fetch transaction histories of all mutual        
        /// </summary>
        [HttpGet("GetPortfolioMutualFundHistory/{UserName}/{Session}/{UserType}/{portfolioId}")]
        public async Task<GenericResponse2> GetPortfolioMutualFundHistory(string UserName, string Session, string UserType, int portfolioId,[FromQuery] string startDate,[FromQuery] string endDate,[FromQuery] int skip,[FromQuery] int pageSize)
        {
            _logger.LogInformation("GetPortfolioMutualFundHistory " + UserName);
            GenericResponse2 genericResponse2 = await _portfolioService.GetPortfolioMutualFundHistory(UserName, Session, UserType, portfolioId, startDate, endDate, skip, pageSize);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///Description: The endpoint allows you to fetch transaction histories of wallet         
        /// </summary>
        [HttpGet("GetPortfolioWalletHistory/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetPortfolioWalletHistory(string UserName, string Session, string UserType,[FromQuery]string startDate, [FromQuery]string endDate,[FromQuery] int skip, [FromQuery] int pageSize)
        {
            _logger.LogInformation("GetPortfolioWalletHistory " + UserName);
            GenericResponse2 genericResponse2 = await _portfolioService.GetPortfolioWalletHistory(UserName, Session, UserType,0, startDate, endDate, skip, pageSize);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///    Description: The endpoint allow client to book a Fixed deposit deal.
        ///     This can be done by calling this endpoint once or twice. If
        ///      the client wish to see the details of the transaction like
        /// interest rate etc before subscription, then the first call to the
        /// endpoint will set a property in the request body name
        ///  “showIntrest” to true, this will return the details of the
        /// transaction.Note, this property must be set to false on the
        /// second call to book the deal.
        /// </summary>
        [HttpGet("FixedDepositSubscription/{Session}/{UserType}")]
        public async Task<GenericResponse2> FixeddepositSubscription(string Session,string UserType, FixeddepositSubscriptionDto fixeddepositSubscription)
        {
           // FixeddepositSubscriptionDto
            _logger.LogInformation("FixeddepositSubscription " +JsonConvert.SerializeObject(fixeddepositSubscription));
            GenericResponse2 genericResponse2 = await _portfolioService.FixeddepositSubscription("", Session, UserType, fixeddepositSubscription);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///    Description: The endpoint allow client to book a mutual fund deal.
        /// </summary>
        [HttpPost("MutualFundSubscription/{Session}/{UserType}")]
        public async Task<GenericResponse2> MutualFundSubscription(string Session, string UserType, MutualFundSubscriptionDto MutualFundSubscription)
        {
            _logger.LogInformation("MutualFundSubscription " + JsonConvert.SerializeObject(MutualFundSubscription));
            GenericResponse2 genericResponse2 = await _portfolioService.MutualFundSubscription("",Session,UserType, MutualFundSubscription);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///    Description: The endpoint allow funding of cash account on test.
        /// </summary>
        [HttpPost("FundCashAccount")]
        public async Task<GenericResponse2> FundCashAccount(FundCashAccountDto fundCashAccount)
        {
            _logger.LogInformation("FundCashAccount " + JsonConvert.SerializeObject(fundCashAccount));
            GenericResponse2 genericResponse2 = await _portfolioService.FundCashAccount(fundCashAccount);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        /// Description: The customer investment summary.
        /// </summary>
        [HttpGet("CustomerInvestmentSummary/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> CustomerInvestmentSummary(string UserName,string Session,string UserType)
        {
            _logger.LogInformation("CustomerInvestmentSummary " + JsonConvert.SerializeObject(UserName));
            GenericResponse2 genericResponse2 = await _portfolioService.CustomerInvestmentSummary(UserName,Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        [HttpGet("BrowserView/{filename}")]
        public IActionResult BrowserView(string filename)
        {
            var filePath = Path.Combine(_folderPaths.Uploads, filename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
            //var mimeType = MimeTypes.GetMimeType(filename);
            var mimeType = MimeTypesMap.GetMimeType(filePath);
            //Console.WriteLine("mimeType " + mimeType);
            _logger.LogInformation("browser filepath " + filePath);
            return PhysicalFile(filePath, mimeType);
        }
    }
}


































































































































































