using EntityProject.repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Retailbanking.BL.IServices;
using Retailbanking.Common.CustomObj;
using System.Threading.Tasks;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PinController : ControllerBase
    {
        private readonly IGeneric _genServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IUserRepository userRepository;
        private readonly IUserCredentialsRepository userCredentialsRepository;
        private readonly IPinService _pinManagementService;

        public PinController(IGeneric genServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IUserRepository userRepository, IUserCredentialsRepository userCredentialsRepository, IPinService pinManagementService)
        {
            _genServ = genServ;
            _logger = logger;
            this.userRepository = userRepository;
            this.userCredentialsRepository = userCredentialsRepository;
            _pinManagementService = pinManagementService;
        }

        [HttpPost("ForgotPin/{UserType}")]
        public async Task<GenericResponse2> AssetCapitalInsuranceForgotPin(AssetCapitalInsuranceForgotPin Request, string UserType)
        {
            return await _pinManagementService.AssetCapitalInsuranceForgotPin(Request.UserName,Request.Session,Request.ChannelId,Request.RequestComment,UserType);
        }

        [HttpPost("ChangePin/{UserType}")]
        public async Task<GenericResponse2> AssetCapitalInsuranceChangePin(CustomerPin Request, string UserType)
        {
            return await _pinManagementService.AssetCapitalInsuranceChangePin(Request,UserType);
        }
    }
}













































































































































































































































































