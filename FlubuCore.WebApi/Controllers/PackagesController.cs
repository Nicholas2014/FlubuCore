﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FlubuCore.WebApi.Controllers.Exceptions;
using FlubuCore.WebApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace FlubuCore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class PackagesController : ControllerBase
    {
        private readonly string[] _allowedFileExtension = { ".zip", ".7z", ".rar" };

        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly ILogger<PackagesController> _logger;

        public PackagesController(IHostingEnvironment hostingEnvironment, ILogger<PackagesController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadPackage()
        {
            if (!Request.HasFormContentType)
            {
                throw new HttpError(HttpStatusCode.BadRequest, "FormHasNoContentType");
            }

            var form = Request.Form;

            if (form == null || form.Files.Count == 0)
            {
                throw new HttpError(HttpStatusCode.BadRequest, "NoFiles");
            }

            var uploadDirectory = GetUploadDirectory();

            foreach (var formFile in form.Files)
            {
                var fileExtension = Path.GetExtension(formFile.FileName);
                if (!_allowedFileExtension.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                {
                    if (form.Files.Count == 1)
                    {
                        throw new HttpError(HttpStatusCode.Forbidden, "FileExtensionNotAllowed", $"File extension {fileExtension} not allowed.");
                    }
                }

                if (formFile.Length > 0)
                {
                    var uploadPath = Path.Combine(uploadDirectory, formFile.FileName);
                    using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(fileStream);
                    }

                    _logger.LogInformation($"Uploaded {uploadPath}");
                }
            }

            return Ok();
        }

        [HttpDelete]
        public IActionResult CleanPackagesDirectory([FromBody]CleanPackagesDirectoryRequest request)
        {
            var uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "packages");

            if (!string.IsNullOrWhiteSpace(request.SubDirectoryToDelete))
            {
                uploadDirectory = Path.Combine(uploadDirectory, request.SubDirectoryToDelete);
            }

            try
            {
                if (Directory.Exists(uploadDirectory))
                {
                    Directory.Delete(uploadDirectory, true);
                }
            }
            catch (IOException)
            {
                Thread.Sleep(1000);
                Directory.Delete(uploadDirectory, true);
            }

            Directory.CreateDirectory(uploadDirectory);

            return Ok();
        }

        private string GetUploadDirectory()
        {
            var form = Request.Form;
            string uploadDirectory = "packages";
            if (form.ContainsKey("request"))
            {
                StringValues request = form["request"];
                var json = request[0];
                try
                {
                    var uploadPackageRequest = JsonConvert.DeserializeObject<UploadPackageRequest>(json);
                    if (!string.IsNullOrWhiteSpace(uploadPackageRequest.UploadToSubDirectory))
                    {
                        uploadDirectory = Path.Combine(uploadDirectory, uploadPackageRequest.UploadToSubDirectory);
                    }
                }
                catch (System.Exception e)
                {
                    _logger.LogWarning(
                        $"request was present but was not of type UploadPackageRequest. Package will be uploaded to root directory. Excetpion: {e}");
                }
            }

            uploadDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, uploadDirectory);

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            return uploadDirectory;
        }
    }
}
