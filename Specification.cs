namespace EFSpecRepro.Specifications
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    // https://fabiomarreco.github.io/blog/2018/specificationpattern-with-entityframework/
    public interface ISpecification<T>
    {
        Boolean IsSatisfiedBy(T entity);
    }

    public interface IQueryableSpecification<T> : ISpecification<T>
    {
        Expression<Func<T, Boolean>> Predicate { get; }
    }

    public abstract class AbstractSpecification<TDomainModel> : IQueryableSpecification<TDomainModel>
    {
        private Func<TDomainModel, Boolean> _isSatisfiedBy;

        public abstract Expression<Func<TDomainModel, Boolean>> Predicate { get; }

        public Boolean IsSatisfiedBy(TDomainModel entity)
        {
            if (_isSatisfiedBy is null)
                _isSatisfiedBy = Predicate.Compile();

            return _isSatisfiedBy(entity);
        }

        public static implicit operator Expression<Func<TDomainModel, Boolean>>(AbstractSpecification<TDomainModel> spec)
            => spec.Predicate;

        public static AbstractSpecification<TDomainModel> operator &(AbstractSpecification<TDomainModel> left, AbstractSpecification<TDomainModel> right)
            => CombineSpecification(left, right, Expression.AndAlso);

        public static AbstractSpecification<TDomainModel> operator |(AbstractSpecification<TDomainModel> left, AbstractSpecification<TDomainModel> right)
            => CombineSpecification(left, right, Expression.OrElse);

        public static AbstractSpecification<TDomainModel> operator !(AbstractSpecification<TDomainModel> spec)
        {
            var predicate = spec.Predicate;
            var newExpr = Expression.Lambda<Func<TDomainModel, bool>>(Expression.Not(predicate.Body), predicate.Parameters[0]);
            return new ConstructedSpecification<TDomainModel>(newExpr);
        }

        protected static AbstractSpecification<TDomainModel> CombineSpecification(AbstractSpecification<TDomainModel> left, AbstractSpecification<TDomainModel> right, Func<Expression, Expression, BinaryExpression> combiner)
        {
            var lExpr = left.Predicate;
            var rExpr = right.Predicate;
            var param = Expression.Parameter(typeof(TDomainModel));
            var combined = combiner.Invoke(
                    new ReplaceParameterVisitor { { lExpr.Parameters.Single(), param } }.Visit(lExpr.Body),
                    new ReplaceParameterVisitor { { rExpr.Parameters.Single(), param } }.Visit(rExpr.Body)
                );
            return new ConstructedSpecification<TDomainModel>(Expression.Lambda<Func<TDomainModel, Boolean>>(combined, param));
        }

        protected class ConstructedSpecification<T> : AbstractSpecification<T>
        {
            private readonly Expression<Func<T, Boolean>> _expr;

            public ConstructedSpecification(Expression<Func<T, Boolean>> specificationExpression)
            {
                _expr = specificationExpression;
            }

            public override Expression<Func<T, Boolean>> Predicate => _expr;
        }
    }

    public static class SpecificationExtensions
    {
        public static AbstractSpecification<T> And<T>(this AbstractSpecification<T> left, AbstractSpecification<T> right) => left & right;

        public static AbstractSpecification<T> Or<T>(this AbstractSpecification<T> left, AbstractSpecification<T> right) => left | right;
    }

    internal class ReplaceParameterVisitor : ExpressionVisitor, IEnumerable<KeyValuePair<ParameterExpression, ParameterExpression>>
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _map = new Dictionary<ParameterExpression, ParameterExpression>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_map.TryGetValue(node, out var newValue))
                return newValue;

            return node;
        }

        public void Add(ParameterExpression parameterToReplace, ParameterExpression replaceWith)
            => _map.Add(parameterToReplace, replaceWith);

        public IEnumerator<KeyValuePair<ParameterExpression, ParameterExpression>> GetEnumerator()
            => _map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
