using System;

namespace Fabric.Identity.API.Exceptions
{
    public class BadRequestException<T> : Exception
    {
        private readonly T _model;

        public BadRequestException()
        {
        }

        public BadRequestException(T model)
        {
            _model = model;
        }

        public BadRequestException(T model, string message) : base(message)
        {
            _model = model;
        }

        public BadRequestException(T model, string message, Exception inner) : base(message, inner)
        {
            _model = model;
        }

        public override string ToString()
        {
            return $"Exeption => {this} --------------- Model => {_model}";
        }
    }
}