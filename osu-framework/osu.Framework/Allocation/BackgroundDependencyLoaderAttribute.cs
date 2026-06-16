// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Statistics;
using osu.Framework.Utils;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Marks a method as the (potentially asynchronous) initialization method of a <see cref="Graphics.Drawable"/>, allowing for automatic injection of dependencies via the parameters of the method.
    /// </summary>
    [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    [AttributeUsage(AttributeTargets.Method)]
    public class BackgroundDependencyLoaderAttribute : Attribute
    {
        private static readonly GlobalStatistic<int> count_reflection_attributes = GlobalStatistics.Get<int>("Dependencies", "Reflected [BackgroundDependencyLoader]s");

        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private bool permitNulls { get; }

        /// <summary>
        /// Marks this method as the (potentially asynchronous) initializer for a class in the context of dependency injection.
        /// </summary>
        public BackgroundDependencyLoaderAttribute()
        {
        }

        /// <summary>
        /// Marks this method as the (potentially asynchronous) initializer for a class in the context of dependency injection.
        /// </summary>
        /// <param name="permitNulls">If true, the initializer may be passed null for the dependencies we can't fulfill.</param>
        public BackgroundDependencyLoaderAttribute(bool permitNulls)
        {
            this.permitNulls = permitNulls;
        }

        internal static InjectDependencyDelegate CreateActivator(Type type)
        {
            count_reflection_attributes.Value++;

            MethodInfo? loaderMethod = null;
            BackgroundDependencyLoaderAttribute? loaderAttribute = null;

            foreach (var method in type.GetMethods(activator_flags))
            {
                var attribute = method.GetCustomAttribute<BackgroundDependencyLoaderAttribute>();
                if (attribute == null)
                    continue;

                if (loaderMethod != null)
                    throw new MultipleDependencyLoaderMethodsException(type);

                loaderMethod = method;
                loaderAttribute = attribute;
            }

            if (loaderMethod == null)
                return (_, _) => { };

            var modifier = loaderMethod.GetAccessModifier();
            if (modifier != AccessModifier.Private)
                throw new AccessModifierNotAllowedForLoaderMethodException(modifier, loaderMethod);

            bool permitNulls = loaderAttribute.permitNulls;
            var parameterInfos = loaderMethod.GetParameters();
            var parameterGetters = new Func<IReadOnlyDependencyContainer, object>[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var parameter = parameterInfos[i];
                parameterGetters[i] = getDependency(parameter.ParameterType, type, permitNulls || parameter.IsNullable());
            }

            return (target, dc) =>
            {
                try
                {
                    object[] parameterArray = new object[parameterGetters.Length];
                    for (int i = 0; i < parameterGetters.Length; i++)
                        parameterArray[i] = parameterGetters[i](dc);

                    loaderMethod.Invoke(target, parameterArray);
                }
                catch (TargetInvocationException exc) // During non-await invocations
                {
                    ExceptionDispatchInfo.Capture(exc.InnerException ?? exc).Throw();
                }
            };
        }

        private static Func<IReadOnlyDependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls)
            => dc => SourceGeneratorUtils.GetDependency(dc, type, requestingType, null, null, permitNulls, false);
    }
}
