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
            this.pathService = pathService;
            this.devicesService = devicesService;
            this.logger = logger;
            this.streamService = streamService;
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
        public IActionResult GetManifestUrl([FromRoute] Guid id)
        {
            var manifestPath = this.pathService.GetDeviceDashManifest(id);
            if (!ConnectionService.UrlExistsAsync(manifestPath).Result)
            {
                logger.LogWarning($"Cannot find MPEG-DASH manifest with id - {id}");
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
            if (devicesService.GetDevice(id).Result.StreamStatus == false)
                return NotFound();

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

            var processInfo = new ProcessStartInfo(
                "/bin/bash",
                $@"sudo ffmpeg -y -i {rtmp} -frames:v {count} {this.pathService.FramesDirectory}/{StreamService.GetFramesFilePattern(id)}.jpg");
            using (var p = Process.Start(processInfo))
            {
                try
                {
                    p?.WaitForExit();
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            return true;
        }
    }
}