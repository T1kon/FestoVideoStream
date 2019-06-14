﻿using FestoVideoStream.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace FestoVideoStream.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// The stream controller.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private readonly StreamService streamService;
        private readonly PathService pathService;
        private readonly DevicesService devicesService;

        private readonly ILogger<StreamController> logger;

        public StreamController(StreamService streamService, PathService pathService, DevicesService devicesService, ILogger<StreamController> logger)
        {
            this.streamService = streamService;
            this.pathService = pathService;
            this.devicesService = devicesService;
            this.logger = logger;
        }

        /// GET: api/stream/1/dash
        /// <summary>
        /// Get MPEG-DASH manifest url.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet("{id}/dash")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetDashManifestUrl([FromRoute] Guid id)
        {
            var manifestPath = this.pathService.GetDeviceDashManifest(id);
            if (!ConnectionService.UrlExistsAsync(manifestPath).Result)
            {
                logger.LogWarning($"Cannot find MPEG-DASH manifest with id - {id}");
                return NotFound();
            }

            return Ok(manifestPath);
        }


        /// GET: api/stream/1/dash
        /// <summary>
        /// Get MPEG-DASH manifest url.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet("{id}/hls")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetHlsManifestUrl([FromRoute] Guid id)
        {
            var manifestPath = this.pathService.GetDeviceHlsManifest(id);
            logger.LogInformation($"Checking manifest file existence at {manifestPath}");
            if (!ConnectionService.UrlExistsAsync(manifestPath).Result)
            {
                logger.LogWarning($"Cannot find HLS manifest with id - {id}");
                return NotFound();
            }

            return Ok(manifestPath);
        }

        /// GET: api/stream/1/rtmp
        /// <summary>
        /// Get RTMP stream url.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet("{id}/rtmp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetRtmpStreamUrl([FromRoute] Guid id)
        {
            var rtmpPath = this.pathService.GetDeviceRtmpPath(id);

            return Ok(rtmpPath);
        }

        /// GET: api/stream/1/frames/5
        /// 
        /// <summary>
        /// Get frames from video stream.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet("{id}/frames/{count}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetFrames([FromRoute] Guid id, [FromRoute] int count)
        {
            logger.LogInformation("Checking device stream status...");
            if (devicesService.GetDevice(id).Result.StreamStatus == false)
            {
                logger.LogWarning($"Stream {id} is offline");
                return NotFound(this.streamService.GetFilesUri(Guid.Empty, count));
            }
            logger.LogInformation("Trying to create frames from stream");
            var result = this.CreateFrames(id, count);
            return result == true ?
                       (IActionResult) Ok(this.streamService.GetFilesUri(id, count)) :
                       BadRequest();
        }

        /// <summary>
        /// Create frames.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        private bool CreateFrames(Guid id, int count)
        {
            var rtmp = this.pathService.GetDeviceRtmpPath(id);
            this.logger.LogInformation($"Creating frames from {rtmp}");
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \" ffmpeg -y -i {rtmp} -frames:v {count} {this.pathService.FramesDirectory}/{StreamService.GetFramesFilePattern(id)}.jpg \"",
                UseShellExecute = false,
                RedirectStandardInput = true
            };
            using (var p = Process.Start(processInfo))
            {
                try
                {
                    p?.WaitForExit();
                    logger.LogInformation($"{count} frames created successfully");
                }
                catch (Exception e)
                {
                    logger.LogInformation($"Error while creating frames");

                    return false;
                }
            }

            return true;
        }

    }
}