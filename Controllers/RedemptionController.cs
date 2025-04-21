using EntityProject.repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Retailbanking.BL.IServices;
using Retailbanking.Common.CustomObj;
using System;
using System.Threading.Tasks;
using MutualFundLiquidationEntity = EntityProject.entities.SimplexLiquidationServiceRequest;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RedemptionController : ControllerBase
    {
        private readonly IRedemptionService redemptionService;
        private readonly ILogger<IRedemptionService> _logger;
        private readonly ISimplexLiquidationServiceRequestRepository _mutualLiquidationService;

        public RedemptionController(ISimplexLiquidationServiceRequestRepository mutualLiquidationService, IRedemptionService redemptionService, ILogger<IRedemptionService> logger)
        {
            this.redemptionService = redemptionService;
            _logger = logger;
            _mutualLiquidationService = mutualLiquidationService;
        }


        /// <summary>
        ///Description: The endpoint to mutual liquidation service request
        ///The respective mutual fund investment should have a reference to track the liquidation
        ///so that people does not litter the service with repeated mail request
        /// </summary>
        [HttpPost("MutualFundLiquidationServiceRequest")]
        public async Task<GenericResponse2> MutualFundLiquidationServiceRequest(SimplexLiquidationServiceRequest simplexLiquidationServiceRequest)
        {
            _logger.LogInformation("SimplexLiquidationServiceRequest " + JsonConvert.SerializeObject(simplexLiquidationServiceRequest));
            MutualFundLiquidationEntity mutualFundLiquidationEntity =
                 _mutualLiquidationService.GetSimplexLiquidationServiceRequestByUserNameAndAccountNumberAndReference(simplexLiquidationServiceRequest.UserName,
                 simplexLiquidationServiceRequest.cutomerBankDetails.RedemptionAccount,
                 simplexLiquidationServiceRequest.UserType,
                 simplexLiquidationServiceRequest.InvestmentId,
                 simplexLiquidationServiceRequest.PartialOrFull);
            if(mutualFundLiquidationEntity==null)
            {
                GenericResponse2 genericResponse2 = await redemptionService.LiquidationServiceRequest(simplexLiquidationServiceRequest.Session,
                    simplexLiquidationServiceRequest.cutomerBankDetails.BankName,
                    simplexLiquidationServiceRequest.cutomerBankDetails.RedemptionAccount,
                    simplexLiquidationServiceRequest.Amount,
                    simplexLiquidationServiceRequest.UserName,
                    simplexLiquidationServiceRequest.UserType,
                    simplexLiquidationServiceRequest.cutomerBankDetails.AccountName);
                if (genericResponse2.Success)
                {
                    mutualFundLiquidationEntity = new MutualFundLiquidationEntity();
                    mutualFundLiquidationEntity.InvestmentId = simplexLiquidationServiceRequest.InvestmentId;
                    mutualFundLiquidationEntity.RedemptionAccount = simplexLiquidationServiceRequest.cutomerBankDetails.RedemptionAccount;
                    mutualFundLiquidationEntity.UserName = simplexLiquidationServiceRequest.UserName;
                    mutualFundLiquidationEntity.CreatedAt = DateTime.Now;
                    mutualFundLiquidationEntity.Amount=simplexLiquidationServiceRequest.Amount;
                    mutualFundLiquidationEntity.BankName = simplexLiquidationServiceRequest.cutomerBankDetails.BankName;
                    mutualFundLiquidationEntity.PartialOrFull = simplexLiquidationServiceRequest.PartialOrFull;
                    _mutualLiquidationService.AddSimplexLiquidationServiceRequest(mutualFundLiquidationEntity);
                    _mutualLiquidationService.AddSimplexLiquidationServiceRequest(mutualFundLiquidationEntity);
                }
                return genericResponse2;
            }
            return new GenericResponse2() { Response=EnumResponse.LiquidationInProcessalready,Message="in process or completed "};
        }
    }
}


































































































































































































































