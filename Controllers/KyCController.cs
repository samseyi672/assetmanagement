using EntityProject.repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Retailbanking.BL.IServices;
using Retailbanking.BL.Services;
using Retailbanking.Common.CustomObj;
using System.Threading.Tasks;
using UsersEntity = EntityProject.entities.Users;
using RegistrationEntity = EntityProject.entities.Registration;
using CustomerDataNotFromBvnEntity = EntityProject.entities.CustomerDataNotFromBvn;
using IdCardEntity = EntityProject.entities.IdCard;
using SignatureEntity = EntityProject.entities.Signature;
using UtilityBillEntity = EntityProject.entities.UtilityBill;
using BvnValidationEntity = EntityProject.entities.BvnValidation;
using Org.BouncyCastle.Ocsp;
using Retailbanking.Common.DbObj;
using Microsoft.AspNetCore.Http;
using System;
using EntityProject.entities;
using Microsoft.Extensions.Options;
using System.IO;
using System.Globalization;

namespace assetmanagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KyCController : ControllerBase
    {
        private readonly IGenericAssetCapitalInsuranceCustomerService _customerServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IIdCardRepository idCardRepository;
        private readonly IUtilityBillRepository utilityBillRepository;
        private readonly ISignatoryRepository signatoryRepository;
        private  readonly IAssetCapitalInsuranceKycService _capitalInsuranceKycService;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IBvnValidationRepository _bvnValidationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICustomerDataNotFromBvnRepository _customerDataFromBvnRepository;
        private readonly IFileService _fileService;
        private readonly AppSettings _settings;

        public KyCController(IBvnValidationRepository bvnValidationRepository,IOptions<AppSettings> settinggs, IFileService fileService,IGenericAssetCapitalInsuranceCustomerService customerServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IIdCardRepository idCardRepository, IUtilityBillRepository utilityBillRepository, ISignatoryRepository signatoryRepository, IAssetCapitalInsuranceKycService capitalInsuranceKycService, IRegistrationRepository registrationRepository, IUserRepository userRepository, ICustomerDataNotFromBvnRepository customerDataFromBvnRepository)
        {
            _bvnValidationRepository=bvnValidationRepository;
            _customerServ = customerServ;
            _logger = logger;
            this.idCardRepository = idCardRepository;
            this.utilityBillRepository = utilityBillRepository;
            this.signatoryRepository = signatoryRepository;
            _capitalInsuranceKycService = capitalInsuranceKycService;
            _registrationRepository = registrationRepository;
            _userRepository = userRepository;
            _customerDataFromBvnRepository = customerDataFromBvnRepository;
            _fileService = fileService;
            _settings = settinggs.Value;
        }

        /// <summary>
        ///Description: The endpoint allows you to update customer details, 
        ///Dont supply firstname,lastname,dob or dateofbirth
        /// </summary>
        [HttpPost("UpdateCustomerDetail/{UserName}/{UserType}")]
        public async Task<GenericResponse2> UpdateCustomerDetail(string UserName,string UserType,ExtendedSimplexCustomerUpdate extendedSimplexCustomerRegistration)
        {
            _logger.LogInformation("UpdateCustomerDetail " + JsonConvert.SerializeObject(extendedSimplexCustomerRegistration));
            UsersEntity usersEntity = _userRepository.GetUserByUserNameAndUserType(UserName, UserType);
            RegistrationEntity registrationEntity = _registrationRepository.GetRegistrationByBvnAndUserType(usersEntity.Bvn, usersEntity.UserType);
            BvnValidationEntity bvnValidation = _bvnValidationRepository.GetBvnValidationByBvn(registrationEntity.Bvn);
            string formattedDate = bvnValidation.DOB.HasValue
                ? bvnValidation.DOB.Value.ToString("dd/MM/yyyy")
                : string.Empty;
            if (usersEntity==null||string.IsNullOrEmpty(usersEntity?.Bvn))
            {
             return new GenericResponse2() { Response=EnumResponse.UserNotFound,Message="User not found"};
            }
            CustomerDataNotFromBvnEntity customer = _customerDataFromBvnRepository.GetCustomerDataNotFromBvnByUserIdAndUserType(usersEntity.id, UserType);
            extendedSimplexCustomerRegistration.birth_date= formattedDate;
            extendedSimplexCustomerRegistration.firstName = usersEntity.FirstName;
            extendedSimplexCustomerRegistration.lastName= usersEntity.LastName;
            extendedSimplexCustomerRegistration.email = registrationEntity.email;
            // extendedSimplexCustomerRegistration.email=
            GenericResponse2 genericResponse2 = await _capitalInsuranceKycService.UpdateCustomerDetail(UserName,UserType, (int)usersEntity.ClientUniqueRef,extendedSimplexCustomerRegistration);
            if(genericResponse2.Success) {
                registrationEntity.sourceOfFund=extendedSimplexCustomerRegistration?.sourceOfFund;
                registrationEntity.Address=extendedSimplexCustomerRegistration?.address;
                registrationEntity.email=extendedSimplexCustomerRegistration?.email;
                registrationEntity.title=extendedSimplexCustomerRegistration?.title;
                registrationEntity.idLga=extendedSimplexCustomerRegistration?.idLga;
                registrationEntity.employerCode =extendedSimplexCustomerRegistration?.employerCode;
                registrationEntity.gender=extendedSimplexCustomerRegistration?.gender;
                registrationEntity.idCountry=extendedSimplexCustomerRegistration?.idCountry;
                registrationEntity.idReligion= (int)(extendedSimplexCustomerRegistration?.idReligion);
                registrationEntity.idState=extendedSimplexCustomerRegistration?.idState;
                registrationEntity.maritalStatus=extendedSimplexCustomerRegistration?.maritalStatus;
                registrationEntity.maidenName=extendedSimplexCustomerRegistration?.maidenName;
                _registrationRepository.AddRegistration(registrationEntity);
                if(!string.IsNullOrEmpty(extendedSimplexCustomerRegistration.phoneNumber))
                {
                    if(customer==null)
                    {
                        customer = new CustomerDataNotFromBvnEntity();
                        customer.PhoneNumber = extendedSimplexCustomerRegistration.phoneNumber;
                        customer.UserName = UserName;
                        customer.Email = extendedSimplexCustomerRegistration?.email;
                        _customerDataFromBvnRepository.AddCustomerDataNotFromBvn(customer);
                    }
                    else
                    {
                        customer.PhoneNumber = extendedSimplexCustomerRegistration.phoneNumber;
                        customer.UserName = UserName;
                        customer.Email = extendedSimplexCustomerRegistration?.email;
                        _customerDataFromBvnRepository.AddCustomerDataNotFromBvn(customer);
                    }
                    
                }
                return genericResponse2;
             }
            genericResponse2.data=JsonConvert.DeserializeObject<SimplexCustomerRegistrationResponse>((string)genericResponse2?.data);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns customer data after registration
        /// </summary>
        [HttpGet("GetCustomerDetailAfterRegistration/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetCustomerDetailAfterRegistration(string UserName,string Session, string UserType)
        {
            _logger.LogInformation("GetRelationship " + Session);
            GenericResponse2 genericResponse = await _capitalInsuranceKycService.GetCustomerDetailAfterRegistration(UserName,Session,UserType);
            RegistrationEntity registrationEntity =_registrationRepository.GetRegistrationByUserNameAndUserType(UserName,UserType);
            if(genericResponse.Success)
            {
                genericResponse.data = registrationEntity;
                return genericResponse;
            }
            return genericResponse;
        }


        /// <summary>
        ///  Description: The endpoint to add idcard or signature or utilitybill
        ///  for DocumentType,you can pass either idcard or signature, or utilitybill
        ///  Remember: this is form-data but not Json request so 
        ///  is better you use postman instead of swagger
        /// </summary>
        [HttpPost("AddUtilityBillOrIdCardOrSignature")]
        [Consumes("multipart/form-data")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<GenericResponse2> AddUtilityBillOrIdCardOrSignature([FromForm] string Session, [FromForm] string UserType, [FromForm] string UserName, [FromForm] string DocumentType, [FromForm] IFormFile file)
        {
       
            if(DocumentType.Equals("idcard",StringComparison.CurrentCultureIgnoreCase))
            {
                var resp = await _capitalInsuranceKycService.AddUtilityBillOrIdCardOrSignature(Session, UserType, UserName, DocumentType,file);
                Console.WriteLine("AddUtilityBillOrIdCardOrSignature ...." + resp.ToString());
                _logger.LogInformation($"resp {resp.ToString()}");
                if(resp.Success)
                {
                    string path = null;
                    IdCardEntity idCard1=idCardRepository.GetIdCardByUserNameAndUserType(UserName,UserType);
                    if(idCard1 != null)
                    {
                        if (_settings.FileUploadPath == "wwwroot")
                        {
                            path = "./" + _settings.FileUploadPath + "/";
                        }
                        else
                        {
                            path = _settings.FileUploadPath + "\\";
                        }
                        _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                        Console.WriteLine("idcard name " + Path.GetFileName(path));
                        _logger.LogInformation("idcard name " + Path.GetFileName(path), null);
                        // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                        string filepath =await _fileService.SaveFileAsync(file, path); // save to external folder
                        idCard1.Filepath = filepath;
                        idCard1.inititated = 0;
                        idCard1.approvalstatus = false;
                        idCardRepository.AddIdCard(idCard1);
                        return resp;
                    }
                    IdCardEntity idCardEntity = new IdCardEntity();
                    idCardEntity.UserType = UserType;
                    idCardEntity.UserName = UserName;               
                    if (_settings.FileUploadPath == "wwwroot")
                    {
                        path = "./" + _settings.FileUploadPath + "/";
                    }
                    else
                    {
                        path = _settings.FileUploadPath + "\\";
                    }
                    _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                    Console.WriteLine("idcrd name " + Path.GetFileName(path));
                    _logger.LogInformation("idcard name " + Path.GetFileName(path), null);
                   // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                    string filepath1 =await _fileService.SaveFileAsync(file, path); // save to external folder
                    idCardEntity.Filepath = filepath1;
                    idCardRepository.AddIdCard(idCardEntity);
                    return resp;
                }
                return resp;
            }else if (DocumentType.Equals("signature", StringComparison.CurrentCultureIgnoreCase))
            {
                var resp = await _capitalInsuranceKycService.AddUtilityBillOrIdCardOrSignature(Session, UserType, UserName, DocumentType,file);
                Console.WriteLine("AddUtilityBillOrIdCardOrSignature ...." + resp.ToString());
                _logger.LogInformation($"resp {resp.ToString()}");
                if (resp.Success)
                {
                    string path = null;
                    SignatureEntity signatureEntity = signatoryRepository.GetSignatureByUserNameAndUserType(UserName, UserType);
                    if (signatureEntity != null)
                    {
                        if (_settings.FileUploadPath == "wwwroot")
                        {
                            path = "./" + _settings.FileUploadPath + "/";
                        }
                        else
                        {
                            path = _settings.FileUploadPath + "\\";
                        }
                        _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                       // Console.WriteLine("utilitybill name " + Path.GetFileName(path));
                        _logger.LogInformation("signature name " + Path.GetFileName(path), null);
                        // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                        string filepath2 = await _fileService.SaveFileAsync(file, path); // save to external folder
                        signatureEntity.Filepath = filepath2;
                        signatureEntity.inititated = 0;
                        signatureEntity.approvalstatus = false;
                        signatoryRepository.AddSignature(signatureEntity);
                        return resp;
                    }
                    SignatureEntity signatureEntity1 = new SignatureEntity();
                    signatureEntity1.UserType = UserType;
                    signatureEntity1.UserName = UserName;
                    if (_settings.FileUploadPath == "wwwroot")
                    {
                        path = "./" + _settings.FileUploadPath + "/";
                    }
                    else
                    {
                        path = _settings.FileUploadPath + "\\";
                    }
                    _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                    Console.WriteLine("signature name " + Path.GetFileName(path));
                    _logger.LogInformation("signature name " + Path.GetFileName(path), null);
                    // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                    string filepath = await _fileService.SaveFileAsync(file, path); // save to external folder
                    signatureEntity1.Filepath = filepath;
                    signatoryRepository.AddSignature(signatureEntity1);
                    return resp;
                }
                return resp;
            }
            else if (DocumentType.Equals("utilitybill", StringComparison.CurrentCultureIgnoreCase))
            {
                var resp = await _capitalInsuranceKycService.AddUtilityBillOrIdCardOrSignature(Session, UserType, UserName, DocumentType,file);
                Console.WriteLine("AddUtilityBillOrIdCardOrSignature ...." + resp.ToString());
                _logger.LogInformation($"resp {resp.ToString()}");
                if (resp.Success)
                {
                    string path = null;
                    UtilityBillEntity utilityBillEntity = utilityBillRepository.GetUtilityBillByUserNameAndUserType(UserName, UserType);
                    if (utilityBillEntity != null)
                    {
                        if (_settings.FileUploadPath == "wwwroot")
                        {
                            path = "./" + _settings.FileUploadPath + "/";
                        }
                        else
                        {
                            path = _settings.FileUploadPath + "\\";
                        }
                        _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                        // Console.WriteLine("utilitybill name " + Path.GetFileName(path));
                        _logger.LogInformation("utilitybill name " + Path.GetFileName(path), null);
                        // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                        string filepath =  await _fileService.SaveFileAsync(file, path); // save to external folder
                        utilityBillEntity.Filepath = filepath;
                        utilityBillEntity.inititated = 0;
                        utilityBillEntity.approvalstatus = false;
                        utilityBillRepository.AddUtilityBill(utilityBillEntity);
                        return resp;
                    }
                    UtilityBillEntity utilityBillEntity1 = new UtilityBillEntity();
                    utilityBillEntity1.UserType = UserType;
                    utilityBillEntity1.UserName = UserName;
                    if (_settings.FileUploadPath == "wwwroot")
                    {
                        path = "./" + _settings.FileUploadPath + "/";
                    }
                    else
                    {
                        path = _settings.FileUploadPath + "\\";
                    }
                    _logger.LogInformation("path " + path + " and _settings.FileUploadPath " + _settings.FileUploadPath, null);
                    Console.WriteLine("utilitybill name " + Path.GetFileName(path));
                    _logger.LogInformation("utilitybill name " + Path.GetFileName(path), null);

                    // int utilityBillresult = await processFileUpload("utilityBill", myuserid, customerDocuments, con, utilityBill, path + utilityBill.FileName);
                  string  filepath1 = await _fileService.SaveFileAsync(file, path); // save to external folder
                    utilityBillEntity1.Filepath = filepath1;
                    utilityBillRepository.AddUtilityBill(utilityBillEntity1);
                    return resp;
                }
                return resp;
            }
           return new GenericResponse2() {Response=EnumResponse.WrongFileType,Message="Wrong file type"};
        }

        /// <summary>
        /// Check Kyc status for signature, utlitybill,idcard, and NIN
        /// </summary>
        [HttpGet("CheckKycStatus/{Session}/{Username}/{ChannelId}/{UserType}")]
        public async Task<GenericResponse2> CheckKycStatus([FromHeader] string ClientKey, string Session, string Username, int ChannelId, string UserType)
        {
            var response  = await _capitalInsuranceKycService.ValidateSessinAndUserTypeForKyc(Session,Username,UserType);
            if(!response.Success)
            {
                return new GenericResponse2()
                {
                    Response = response.Response,
                    Success = response.Success,
                    Message = response.Message
                };
            }
            UsersEntity usersEntity = _userRepository.GetUserByUserNameAndUserType(Username, UserType);
            RegistrationEntity registrationEntity = _registrationRepository.GetRegistrationByUserNameAndUserType(Username,UserType);
            if (usersEntity != null)
            {
                //Console.WriteLine("CheckKycStatus ...." + JsonConvert.SerializeObject(usersEntity));
                SignatureEntity signatureEntity = signatoryRepository.GetSignatureByUserNameAndUserType(Username,UserType);
                IdCardEntity idCardEntity = idCardRepository.GetIdCardByUserNameAndUserType(Username, UserType);
                UtilityBillEntity utilityBill = utilityBillRepository.GetUtilityBillByUserNameAndUserType(Username, UserType);
                _logger.LogInformation("idCardEntity " + JsonConvert.SerializeObject(idCardEntity));
                _logger.LogInformation("signatureEntity " + JsonConvert.SerializeObject(signatureEntity));
                _logger.LogInformation("utilityBill " + JsonConvert.SerializeObject(utilityBill));
                var kycstatus = new
                {
                  signature= signatureEntity!=null?(new
                  {
                   isaproved=signatureEntity?.approvalstatus,
                   isadminInitiated=signatureEntity?.inititated,
                    basicinfo = new
                    {
                        nin = registrationEntity?.Nin,
                        bvn = registrationEntity?.Bvn,
                        dob = registrationEntity?.birth_date,
                        jobId = registrationEntity?.occupationId,
                        address = registrationEntity?.Address,
                        religionId = registrationEntity?.idReligion,
                        firstName = registrationEntity?.FirstName,
                        lastName = registrationEntity?.LastName,
                        gender = registrationEntity?.gender,
                    }
                  }):null,
                    idcard = idCardEntity!=null?(new
                    {
                        isaproved = idCardEntity?.approvalstatus,
                        isadminInitiated = idCardEntity?.inititated,
                        basicinfo= new { 
                           nin=registrationEntity?.Nin,
                           bvn=registrationEntity?.Bvn,
                           dob=registrationEntity?.birth_date,
                           job=registrationEntity.occupationId,
                           address=registrationEntity?.Address,
                           religion=registrationEntity?.idReligion,
                           firstName=registrationEntity?.FirstName,
                           lastName=registrationEntity?.LastName,
                           gender=registrationEntity.gender,
                        }
                    }):null,
                    utilityBill = utilityBill!=null?(new
                    {
                        isaproved = utilityBill?.approvalstatus,
                        isadminInitiated = utilityBill?.inititated,
                        basicinfo = new
                        {
                            nin = registrationEntity?.Nin,
                            bvn = registrationEntity?.Bvn,
                            dob = registrationEntity?.birth_date,
                            job = registrationEntity.occupationId,
                            address = registrationEntity?.Address,
                            religion = registrationEntity?.idReligion,
                            firstName = registrationEntity?.FirstName,
                            lastName = registrationEntity?.LastName,
                            gender = registrationEntity.gender,
                        }
                    }):null,
                    nin=registrationEntity!=null?registrationEntity?.Nin:null,
                };
                return new GenericResponse2(){Response=EnumResponse.Successful,Success=true,Message="kyc status",data=kycstatus};
            }
            else
            {
                return new GenericResponse2() { Response = EnumResponse.UserNotFound, Message = "User not found" };
            }
        }
    }

}











































