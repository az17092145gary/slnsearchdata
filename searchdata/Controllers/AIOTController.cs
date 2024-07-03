using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using searchdata.Model;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace searchdata.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class AIOTController : ControllerBase
    {
        private readonly AIOTService _Service;
        private JsonSerializerOptions options;
        public AIOTController(AIOTService service)
        {
            _Service = service;
            options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
        }
        [HttpGet("getOneLineData")]
        public dynamic getOneLineData(string startTime, string endTime, string item, string product, string line, string? reporttype)
        {
            var list = _Service.getOneLineData(startTime, endTime, item, product, line, reporttype);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getOneLineNonTimeData")]
        public dynamic getOneLineNonTimeData(string startTime, string endTime, string item, string product, string line, string device)
        {
            var list = _Service.getOneLineNonTimeData(startTime, endTime, item, product, line, device);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("getMoreLineData")]
        public dynamic getMoreLineData(FrontMoreLineData frontMoreLineData)
        {
            var list = _Service.getMoreLineData(frontMoreLineData);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost("getMoreLinePerformanceData")]
        public dynamic getMoreLinePerformanceData(FrontMoreLineData frontMoreLineData)
        {
            var list = _Service.getMoreLinePerformanceData(frontMoreLineData);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost("getMoreLineERRData")]
        public dynamic getMoreLineERRData(FrontMoreLineData frontMoreLineData)
        {
            var list = _Service.getMoreLineERRData(frontMoreLineData);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getOneLineERROrder")]
        public dynamic getOneLineERROrder(string startTime, string endTime, string item, string product, string line, string type, string reporttype, string device)
        {
            var list = _Service.getOneLineERROrder(startTime, endTime, item, product, line, type, reporttype, device);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getErrData")]
        public dynamic getAVGErrData(string startTime, string endTime, string item, string product, string line, string type, string device, string? avg)
        {
            var list = _Service.getErrData(startTime, endTime, item, product, line, type, device, avg);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getStopTimeTable")]
        public dynamic getStopTimeTable(string startTime, string endTime, string item, string product, string line, string device, string type)
        {
            var list = _Service.getStopTimeTable(startTime, endTime, item, product, line, device, type);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getStopTimeTableERRCode")]
        public dynamic getStopTimeTableERRCode(string strtime, string endtime, string device)
        {
            var list = _Service.getStopTimeTableERRCode(strtime, endtime, device);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list, options);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getAllProduct")]
        public dynamic getAllProduct(string item)
        {
            var list = _Service.getAllProduct(item);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getAllLine")]
        public dynamic getAllLine(string product)
        {
            var list = _Service.getAllLine(product);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getDeviceName")]
        public dynamic getDeviceName(string item, string product, string line)
        {
            var list = _Service.getDeviceName(item, product, line);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getAllItem")]
        public dynamic getAllItem()
        {
            var list = _Service.getAllItem();
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("getSCMData")]
        public dynamic getSCMData()
        {
            var list = _Service.getSCMData();
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost("createORUpdateSCM")]
        public dynamic createORUpdateSCM(SCM scm)
        {
            var check = _Service.createORUpdateSCM(scm);
            return check == true ? Ok() : NotFound();
        }
        [HttpGet("getKanBanProduct")]
        public dynamic getKanBanProduct(string item)
        {
            var list = _Service.getKanBanProduct(item);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpGet("getKanBanData")]
        public dynamic getKanBanData(string item)
        {
            var list = _Service.getKanBanData(item);
            if (list.Count > 0)
            {
                var result = JsonSerializer.Serialize(list);
                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("saveReMark")]
        public dynamic saveReMark(requestSaveReMark reMark)
        {
            var check = _Service.saveReMark(reMark);
            return check == true ? Ok() : NotFound();
        }
    }
}
