﻿using Ac.Net.Authentication;
using Ac.Net.Authentication.Models;

using Flurl.Http;
using Ganss.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NPOI.SS.Formula.Functions;
using System.IO;
using System.Text;
using TestPlugin.Excel;
using TestPlugin.Models;

namespace TestPlugin.Controllers
{
    public class TestBatchController : Controller
    {
        const string operation = "test";
        public const string PATH_TEMPLATE = "api/batch/" + operation + "/template";
        public const string PATH_LOAD = "api/batch/" + operation + "/load";
        [HttpGet]
        [Route(PATH_TEMPLATE)]
        public async Task<ActionResult> GetTemplate()
        {
            try
            {
                var fileName = $"{ExcelUtils.DownloadWorksheet}.Template.{DateTime.Now.ToString("yy.MM.dd.mm.ss")}.xlsx";
                var path = Path.Combine(Path.GetTempPath(), fileName);
                var bytes = ExcelUtils.MockTestDownloadXlsx();
                return File(bytes, "application/vnd.ms-excel");

            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route(PATH_LOAD)]
        public async Task<ActionResult> UploadData([FromForm] IFormFile FileData)
        {

            try
            {
                string name = FileData.FileName;
                string extension = Path.GetExtension(FileData.FileName);

                using (var memoryStream = new MemoryStream())
                {
                    FileData.CopyTo(memoryStream);
                    var items = new ExcelMapper(memoryStream).Fetch<TestDownload>();

                    

                    //await BatchFlows.ProcessDownloadBatch(ThreeLeggedMananger.Instance, DataContext context, items);
                }

                return Ok();

            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }



    }
}



