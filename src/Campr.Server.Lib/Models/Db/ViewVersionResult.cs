﻿using System;
using Campr.Server.Lib.Json;
using Newtonsoft.Json;

namespace Campr.Server.Lib.Models.Db
{
    public class ViewVersionResult
    {
        public string DocId { get; set; }
        
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Date { get; set; }        
    }
}