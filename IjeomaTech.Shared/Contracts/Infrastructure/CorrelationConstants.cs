using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Infrastructure
{
    public static class CorrelationConstants
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string ItemKey = "CorrelationId";
    }
}
