﻿using System;

namespace FestoVideoStream.Dto
{
    public class DeviceDetailsDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string IpAddress { get; set; }

        public string Config { get; set; }

        public bool DeviceStatus { get; set; }

        public DateTime? LastActivityDate { get; set; }

        public bool StreamingStatus { get; set; }

        public DateTime? LastStreamingDate { get; set; }
    }
}