using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Custom_Exceptions
{
    internal class TrainingException : Exception
    {
        public TrainingException() : base() { }
        public TrainingException(string message) : base(message) { }
        public TrainingException(string message, Exception innerException) : base(message, innerException) { }
    }


    internal class RegressionTrainingException : TrainingException
    {
        public RegressionTrainingException() : base() { }
        public RegressionTrainingException(string message) : base(message) { }
        public RegressionTrainingException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    internal class ClassificationTrainingException : TrainingException
    {
        public ClassificationTrainingException() : base() { }
        public ClassificationTrainingException(string message) : base(message) { }
        public ClassificationTrainingException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class ForecastingTrainingException : TrainingException
    {
        public ForecastingTrainingException() : base() { }
        public ForecastingTrainingException(string message) : base(message) { }
        public ForecastingTrainingException(string message, Exception innerException) : base(message, innerException) { }
    }

}
