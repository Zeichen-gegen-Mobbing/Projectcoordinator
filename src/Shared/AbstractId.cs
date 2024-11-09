using System.ComponentModel;

namespace ZgM.ProjectCoordinator.Shared
{
    public abstract class AbstractId<T, BaseType> : IEquatable<AbstractId<T, BaseType>> where T : AbstractId<T, BaseType>
    {
        internal BaseType Id { init; get; }

        protected AbstractId(BaseType id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as AbstractId<T, BaseType>);
        }

        public bool Equals(AbstractId<T, BaseType>? other)
        {
            return other != null && Id!.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id!.GetHashCode();
        }

        public static bool operator ==(AbstractId<T, BaseType>? left, AbstractId<T, BaseType>? right)
        {
            return EqualityComparer<AbstractId<T, BaseType>>.Default.Equals(left, right);
        }

        public static bool operator !=(AbstractId<T, BaseType>? left, AbstractId<T, BaseType>? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Id!.ToString()!;
        }
    }
}