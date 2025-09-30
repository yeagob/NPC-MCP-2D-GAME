using System;

namespace Grid.Models.Grid
{
    [Serializable]
    public struct GridResult<T>
    {
        public bool hasValue;
        public T value;

        public GridResult(T value)
        {
            this.hasValue = true;
            this.value = value;
        }

        public static GridResult<T> Success(T value)
        {
            return new GridResult<T>(value);
        }

        public static GridResult<T> Failure()
        {
            return new GridResult<T>
            {
                hasValue = false,
                value = default(T)
            };
        }

        public bool TryGetValue(out T result)
        {
            result = hasValue ? value : default(T);
            return hasValue;
        }
    }
}