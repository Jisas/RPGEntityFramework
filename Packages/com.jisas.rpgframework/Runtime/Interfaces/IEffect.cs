
namespace RPGFramework
{
    public interface IEffect<TTarget>
    {
        void Apply(TTarget target) { }
        void Cancel() { }
    }
}
