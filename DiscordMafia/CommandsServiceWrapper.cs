using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Reflection.Emit;
using System.Collections;
using DiscordMafia.Config;
using DiscordMafia.Config.Lang;

namespace DiscordMafia
{
    /// <summary>
    /// Partially copy-paste from ModuleClassBuilder
    /// Makes a new dynamic assembly, create localized module classes and registers them in CommandService.
    /// </summary>
    public class CommandsServiceLocalizer
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static readonly TypeInfo _moduleTypeInfo = typeof(ModuleBase).GetTypeInfo();

        public async Task AddModulesAsync(MainSettings settings, CommandService service, Assembly assembly)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var types = await SearchAsync(settings, assembly, service).ConfigureAwait(false);
                foreach (var info in types)
                {
                    await service.AddModuleAsync(info.AsType());
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<IReadOnlyList<TypeInfo>> SearchAsync(MainSettings settings, Assembly assembly, CommandService service)
        {
            var mb = GetModuleBuilder();
            var result = new List<TypeInfo>();

            foreach (var typeInfo in assembly.DefinedTypes)
            {
                if (typeInfo.IsPublic || typeInfo.IsNestedPublic)
                {
                    if (IsValidModuleDefinition(typeInfo) &&
                        !typeInfo.IsDefined(typeof(DontAutoLoadAttribute)))
                    {
                        result.Add(MakeDirt(settings.Language.ModuleMessages, mb, typeInfo));
                    }
                }
                else if (IsLoadableModule(typeInfo))
                {
                    await System.Console.Error.WriteLineAsync($"Class {typeInfo.FullName} is not public and cannot be loaded. To suppress this message, mark the class with {nameof(DontAutoLoadAttribute)}.");
                }
            }

            return result;
        }

        private TypeInfo MakeDirt(ModuleMessages messages, ModuleBuilder mb, TypeInfo typeInfo)
        {
            var type = typeInfo.AsType();
            var tb = GetTypeBuilder(mb, type.Name + "Proxy", type);

            messages.TryGetValue(type.Name, out var moduleLang);

            // copy and localize attributes
            {
                var attrs = PrepareAttributes(typeInfo.CustomAttributes, moduleLang);
                foreach (var attr in attrs)
                {
                    tb.SetCustomAttribute(attr);
                }
            }

            // create constructors with parent call
            foreach (var ctor in type.GetConstructors())
            {
                var paramTypes = ctor.GetParameters().Select(pi => pi.ParameterType).ToArray();
                var newCtor = tb.DefineConstructor(ctor.Attributes, ctor.CallingConvention, paramTypes);
                var gen = newCtor.GetILGenerator();
                GenerateInstanceCall(gen, ctor, paramTypes.Length);
            }

            // create command methods with attributes and parent call
            foreach (var method in typeInfo.DeclaredMethods.Where(m => IsValidCommandDefinition(m)))
            {
                CommandMessages.CommandInfo methodLang = null;
                var commandAttr = method.GetCustomAttribute<CommandAttribute>();
                moduleLang?.Commands?.TryGetValue(commandAttr?.Text ?? method.Name, out methodLang);

                // copy parameter definitions with attributes
                var parameters = method.GetParameters();
                var paramTypes = parameters.Select(pi => pi.ParameterType).ToArray();
                var newMethod = tb.DefineMethod(
                    method.Name + "_Proxy", method.Attributes, method.CallingConvention, method.ReturnType, null, null, paramTypes, null, null
                );
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    // 1-based index, 0 - return value
                    var pb = newMethod.DefineParameter(i + 1, p.Attributes, p.Name);
                    if (p.HasDefaultValue)
                    {
                        pb.SetConstant(p.DefaultValue);
                    }

                    ParameterMessages.ParameterInfo paramLang = null;
                    methodLang?.Parameters?.TryGetValue(p.Name, out paramLang);

                    // copy and localize attributes
                    {
                        var attrs = PrepareAttributes(p.CustomAttributes, paramLang);
                        foreach (var attr in attrs)
                        {
                            pb.SetCustomAttribute(attr);
                        }
                    }
                }
                var gen = newMethod.GetILGenerator();
                GenerateInstanceCall(gen, method, paramTypes.Length);

                // copy and localize attributes
                {
                    var attrs = PrepareAttributes(method.CustomAttributes, methodLang);
                    foreach (var attr in attrs)
                    {
                        newMethod.SetCustomAttribute(attr);
                    }
                }
            }
            TypeInfo objectTypeInfo = tb.CreateTypeInfo();
            return objectTypeInfo;
        }

        private static IEnumerable<CustomAttributeBuilder> PrepareAttributes(IEnumerable<CustomAttributeData> attrs, ISummarized lang)
        {
            var result = new List<CustomAttributeBuilder>();
            bool hasSummaryAttr = false;
            bool hasAliasAttr = false;

            foreach (var attr in attrs)
            {
                object[] ctorArgs = attr.ConstructorArguments.Select(
                        a =>
                        {
                            if (a.Value is IEnumerable<CustomAttributeTypedArgument> col)
                            {
                                return col.Select(b => b.Value).ToArray();
                            }
                            return a.Value;
                        }
                    ).ToArray();
                if (attr.AttributeType == typeof(AliasAttribute))
                {
                    hasAliasAttr = true;
                    if (lang?.Aliases != null && lang.Aliases.Length > 0)
                    {
                        ctorArgs = new object[] { lang.Aliases };
                    }
                }
                else if (attr.AttributeType == typeof(SummaryAttribute))
                {
                    hasSummaryAttr = true;
                    if (!string.IsNullOrEmpty(lang?.Summary))
                    {
                        ctorArgs = new object[] { lang.Summary };
                    }
                }
                var atb = new CustomAttributeBuilder(
                    attr.Constructor,
                    ctorArgs
                );
                result.Add(atb);
            }
            if (!hasSummaryAttr && !string.IsNullOrEmpty(lang?.Summary))
            {
                var atb = new CustomAttributeBuilder(
                    typeof(SummaryAttribute).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { lang.Summary }
                );
                result.Add(atb);
            }
            if (!hasAliasAttr && lang?.Aliases != null && lang.Aliases.Length > 0)
            {
                var atb = new CustomAttributeBuilder(
                    typeof(AliasAttribute).GetConstructor(new Type[] { typeof(string[]) }),
                    new object[] { lang.Aliases }
                );
                result.Add(atb);
            }
            return result;
        }

        private static bool IsLoadableModule(TypeInfo info)
        {
            return info.DeclaredMethods.Any(x => x.GetCustomAttribute<CommandAttribute>() != null) &&
                info.GetCustomAttribute<DontAutoLoadAttribute>() == null;
        }

        private static bool IsValidModuleDefinition(TypeInfo typeInfo)
        {
            return _moduleTypeInfo.IsAssignableFrom(typeInfo) &&
                   !typeInfo.IsAbstract;
        }
        
        private static bool IsValidCommandDefinition(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(CommandAttribute)) &&
                   (methodInfo.ReturnType == typeof(Task) || methodInfo.ReturnType == typeof(Task<RuntimeResult>)) &&
                   !methodInfo.IsStatic &&
                   !methodInfo.IsGenericMethod;
        }

        private static TypeBuilder GetTypeBuilder(ModuleBuilder moduleBuilder, string typeName, Type parent)
        {
            TypeBuilder tb = moduleBuilder.DefineType(typeName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    parent);
            return tb;
        }

        private static ModuleBuilder GetModuleBuilder()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            return assemblyBuilder.DefineDynamicModule("MainModule");
        }

        private static void GenerateInstanceCall(ILGenerator generator, MethodBase method, int paramCount)
        {
            // this -> stack
            generator.Emit(OpCodes.Ldarg_0);
            for (byte i = 0; i < paramCount; i++)
            {
                switch (i)
                {
                    case 0:
                        // arg 0 -> stack (optimized)
                        generator.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        // arg 1 -> stack (optimized)
                        generator.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        // arg 2 -> stack (optimized)
                        generator.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        // other arg -> stack
                        generator.Emit(OpCodes.Ldarg_S, i);
                        break;
                }
            }
            // call method or ctor
            if (method is MethodInfo m)
            {
                generator.Emit(OpCodes.Call, m);
            }
            else if (method is ConstructorInfo c)
            {
                generator.Emit(OpCodes.Call, c);
            }
            else
            {
                throw new ArgumentException("Method must be an instance of MethodInfo or Constructor Info", "method");
            }
            // return value -> stack (if any) and return
            generator.Emit(OpCodes.Ret);
        }
    }
}
