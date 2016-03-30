using System;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentRequestDate
    {
        DateTime Date { get; }
        string Version { get; }
    }
}