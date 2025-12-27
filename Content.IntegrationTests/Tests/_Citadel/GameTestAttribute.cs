#nullable enable
using System.Collections.Generic;
using System.Reflection;
using Content.IntegrationTests.Pair;
using JetBrains.Annotations;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Citadel;

// oh man some of this code spooky. :(

/// <summary>
///     Marks a game test, that needs a client and server to run.
/// </summary>
/// <typeparam name="TData">The GameTestData inheriter to use.</typeparam>
[MeansImplicitUse]
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class GameTestAttribute<TData> : Attribute, ITestBuilder, IImplyFixture, IApplyToTest, ITestData
    where TData: GameTestData, new()
{
    public object?[] Arguments { get; init; }

    /// <summary>
    ///     An optional description of the test.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Which side to run the inner test code on, if not the test thread.
    /// </summary>
    public Side RunOnSide { get; set; } = Side.Neither;

    public GameTestAttribute()
    {
        Arguments = Array.Empty<object?>();
    }

    public GameTestAttribute(params object?[] args)
    {
        Arguments = args;
    }

    /// <summary>
    ///     Evil magic that allows us to cleanly wrap a test method.
    ///     This is used instead of a 'simple' closure because of the need to preserve the object being invoked on.
    /// </summary>
    private sealed class TestDataBasedWrapper(IMethodInfo inner, GameTestAttribute<TData> attribute) : IMethodInfo
    {
        public T[] GetCustomAttributes<T>(bool inherit) where T : class
        {
            return Array.Empty<T>();
        }

        public bool IsDefined<T>(bool inherit) where T : class
        {
            return false;
        }

        public IParameterInfo[] GetParameters()
        {
            return Array.Empty<IParameterInfo>();
        }

        public Type[] GetGenericArguments()
        {
            return Array.Empty<Type>();
        }

        public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new NotSupportedException();
        }

        public object Invoke(object? fixture, params object?[]? args)
        {
            return InnerInvoke(fixture);
        }

        private async Task InnerInvoke(object? fixture)
        {
            var data = new TData();

            await data.DoSetup();

            try
            {
                async Task DoRun()
                {
                    var args = new object[attribute.Arguments.Length + 1];
                    args[0] = data;
                    attribute.Arguments.CopyTo(args, 1);

                    if (inner.ReturnType.IsType(typeof(Task)))
                    {
                        await (Task)inner.Invoke(fixture, args)!;
                    }
                    else
                    {
                        inner.Invoke(fixture, args);
                    }
                }

                if (attribute.RunOnSide is { } side && side != Side.Neither)
                {
                    RobustIntegrationTest.IntegrationInstance
                        instance = side == Side.Client ? data.Pair.Client : data.Pair.Server;

                    await instance.WaitAssertion(() =>
                    {
                        DoRun().Wait();
                    });
                }
                else
                {
                    await DoRun();
                }
            }
            catch (Exception)
            {
                data.MarkDirty();
                throw;
            }
            finally
            {
                await data.DoTeardown();
            }
        }

        public ITypeInfo TypeInfo => new TypeWrapper(((Func<Task>)(HackToLookAsync)).GetType());
        public MethodInfo MethodInfo => ((Func<Task>)(HackToLookAsync)).Method;
        public string Name => inner.Name;
        public bool IsAbstract => false;
        public bool IsPublic => true;
        public bool IsStatic => false;
        public bool ContainsGenericParameters => false;
        public bool IsGenericMethod => false;
        public bool IsGenericMethodDefinition => false;
        public ITypeInfo ReturnType => new TypeWrapper(typeof(void));

        private Task HackToLookAsync()
        {
            return Task.CompletedTask;
        }
    }

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
    {
        var innerParams = method.GetParameters();

        if (innerParams.Length == 1 + Arguments.Length && innerParams[0].ParameterType.IsAssignableTo(typeof(GameTestData)))
        {
            var wrapper = new TestDataBasedWrapper(method, this);

            return new[] { new TestMethod(wrapper, null) };
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void ApplyToTest(Test test)
    {
        if (!test.Properties.ContainsKey(PropertyNames.Description) && Description is not null)
            test.Properties.Set(PropertyNames.Description, Description);
    }

    public string? TestName { get; }
    public RunState RunState { get; } = RunState.Runnable;

    public IPropertyBag Properties { get; } = new PropertyBag();
}

public sealed class DirtyFlag
{
    public bool IsDirty { get; private set; }

    public void Set() => IsDirty = true;
}

/// <summary>
///     A simpler version of the generic GameTestAttribute that allows you to specify what you need with just arguments.
/// </summary>
[MeansImplicitUse]
[PublicAPI]
public sealed class GameTestAttribute : Attribute, ITestBuilder, IImplyFixture, IApplyToTest
{
    /// <summary>
    ///     An optional description of the test.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Which side to run the inner test code on, if not the test thread.
    /// </summary>
    public Side RunOnSide { get; set; }  = Side.Neither;

    /// <summary>
    ///     Evil magic that allows us to cleanly wrap a test method.
    ///     This is used instead of a 'simple' closure because of the need to preserve the object being invoked on.
    /// </summary>
    private sealed class AttributeBasedWrapper(IMethodInfo inner, GameTestAttribute attribute) : IMethodInfo
    {
        public T[] GetCustomAttributes<T>(bool inherit) where T : class
        {
            return Array.Empty<T>();
        }

        public bool IsDefined<T>(bool inherit) where T : class
        {
            return false;
        }

        public IParameterInfo[] GetParameters()
        {
            return Array.Empty<IParameterInfo>();
        }

        public Type[] GetGenericArguments()
        {
            return Array.Empty<Type>();
        }

        public IMethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new NotSupportedException();
        }

        public object Invoke(object? fixture, params object?[]? args)
        {
            return InnerInvoke(fixture);
        }

        private async Task InnerInvoke(object? fixture)
        {
            var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });

            var args = new List<object>();

            var dirty = new DirtyFlag();

            foreach (var param in inner.GetParameters())
            {
                if (param.GetCustomAttributes<SidedDependencyAttribute>(false) is [var dependencyAttribute])
                {
                    if (dependencyAttribute.Side is Side.Server)
                    {
                        args.Add(pair.Server.InstanceDependencyCollection.ResolveType(param.ParameterType));
                    }
                    else
                    {
                        args.Add(pair.Client.InstanceDependencyCollection.ResolveType(param.ParameterType));
                    }
                }
                else if (param.GetCustomAttributes<SystemAttribute>(false) is [var systemAttribute])
                {
                    if (systemAttribute.Side is Side.Server)
                    {
                        args.Add(pair.Server.EntMan.EntitySysManager.GetEntitySystem(param.ParameterType));
                    }
                    else
                    {
                        args.Add(pair.Client.EntMan.EntitySysManager.GetEntitySystem(param.ParameterType));
                    }
                }
                else if (param.ParameterType == typeof(TestPair))
                {
                    args.Add(pair);
                }
                else if (param.ParameterType == typeof(DirtyFlag))
                {
                    args.Add(dirty);
                }
            }


            try
            {
                async Task DoRun()
                {
                    if (inner.ReturnType.IsType(typeof(Task)))
                    {
                        await (Task)inner.Invoke(fixture, args.ToArray())!;
                    }
                    else
                    {
                        inner.Invoke(fixture, args.ToArray());
                    }
                }

                if (attribute.RunOnSide is { } side && side != Side.Neither)
                {
                    RobustIntegrationTest.IntegrationInstance
                        instance = side == Side.Client ? pair.Client : pair.Server;

                    await instance.WaitAssertion(() =>
                    {
                        DoRun().Wait();
                    });
                }
                else
                {
                    await DoRun();
                }

            }
            catch (Exception)
            {
                dirty.Set();
                throw;
            }
            finally
            {
                if (!dirty.IsDirty)
                    await pair.CleanReturnAsync();
                else
                    await pair.DisposeAsync();
            }
        }

        public ITypeInfo TypeInfo => new TypeWrapper(((Func<Task>)(HackToLookAsync)).GetType());
        public MethodInfo MethodInfo => ((Func<Task>)(HackToLookAsync)).Method;
        public string Name => inner.Name;
        public bool IsAbstract => false;
        public bool IsPublic => true;
        public bool IsStatic => false;
        public bool ContainsGenericParameters => false;
        public bool IsGenericMethod => false;
        public bool IsGenericMethodDefinition => false;
        public ITypeInfo ReturnType => new TypeWrapper(typeof(void));

        private Task HackToLookAsync()
        {
            return Task.CompletedTask;
        }
    }

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
    {
        var innerParams = method.GetParameters();

        if (innerParams.Length == 1 && innerParams[0].ParameterType.IsAssignableTo(typeof(GameTestData)))
        {
            throw new NotSupportedException();
        }
        else
        {
            var wrapper = new AttributeBasedWrapper(method, this);

            return new[] { new TestMethod(wrapper, null) };
        }
    }

    public void ApplyToTest(Test test)
    {
        if (!test.Properties.ContainsKey(PropertyNames.Description) && Description is not null)
            test.Properties.Set(PropertyNames.Description, Description);
    }
}
