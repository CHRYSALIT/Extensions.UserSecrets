using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Chrysalit.Extensions.UserSecrets
{
    /// <summary>
    /// Provide additional extension methods to add user secrets bounded to a <see cref="IHostEnvironment"/> to the <see cref="IConfigurationBuilder"/>.
    /// These methods discover file "secrets.{<see cref="IHostEnvironment.EnvironmentName"/>}.json".
    /// </summary>
    public static class UserSecretsConfigurationExtensions
    {
        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. Searches the assembly that contains type <typeparamref name="T"/>
        /// for an instance of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// <para>
        /// Adds the users secrets configuration source depending of the <see cref="IHostingEnvironment"/> provided by <paramref name="hostBuilderContext"/>.
        /// This enable access to per-environment secrets.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="hostBuilderContext">The <see cref="HostBuilderContext"/></param>
        /// <typeparam name="T">The type from the assembly to search for an instance of <see cref="UserSecretsIdAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown when the assembly containing <typeparamref name="T"/> does not have <see cref="UserSecretsIdAttribute"/>.</exception>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, HostBuilderContext hostBuilderContext)
            where T : class
            => configuration.AddUserSecrets<T>(hostBuilderContext.HostingEnvironment);

        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. Searches the assembly that contains type <typeparamref name="T"/>
        /// for an instance of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// <para>
        /// Adds the users secrets configuration source depending of the <see cref="IHostingEnvironment"/> provided by <paramref name="hostEnvironment"/>.
        /// This enable access to per-environment secrets.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="hostEnvironment">The <see cref="IHostingEnvironment"/></param>
        /// <typeparam name="T">The type from the assembly to search for an instance of <see cref="UserSecretsIdAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown when the assembly containing <typeparamref name="T"/> does not have <see cref="UserSecretsIdAttribute"/>.</exception>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, IHostEnvironment hostEnvironment)
            where T : class
            => configuration.AddUserSecrets(typeof(T).Assembly, hostEnvironment, optional: true, reloadOnChange: false);


        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. Searches the assembly that contains type <typeparamref name="T"/>
        /// for an instance of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// <para>
        /// Adds the users secrets configuration source depending of the <see cref="IHostingEnvironment"/> provided by <paramref name="hostEnvironment"/>.
        /// This enable access to per-environment secrets.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="hostEnvironment">The <see cref="IHostingEnvironment"/></param>
        /// <typeparam name="T">The type from the assembly to search for an instance of <see cref="UserSecretsIdAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown when the assembly containing <typeparamref name="T"/> does not have <see cref="UserSecretsIdAttribute"/>.</exception>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration, Func<string> environmentDelegate)
            where T : class
        {
            IHostEnvironment hostEnvironment = new HostingEnvironment { 
                EnvironmentName = environmentDelegate() ?? string.Empty
            };
            return configuration.AddUserSecrets<T>(hostEnvironment);
        }


        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. This searches <paramref name="assembly"/> for an instance
        /// of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="assembly">The assembly with the <see cref="UserSecretsIdAttribute" />.</param>
        /// <param name="hostEnvironment">The <see cref="IHostEnvironment" /> provided if any.</param> 
        /// <param name="optional">Whether loading secrets is optional. When false, this method may throw.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="optional"/> is false and <paramref name="assembly"/> does not have a valid <see cref="UserSecretsIdAttribute"/>.</exception>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly, IHostEnvironment? hostEnvironment, bool optional, bool reloadOnChange)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ = configuration ?? throw new ArgumentNullException(nameof(assembly));

            UserSecretsIdAttribute? attribute = assembly.GetCustomAttribute<UserSecretsIdAttribute>();
            if (attribute != null)
            {
                return AddUserSecretsInternal(configuration, attribute.UserSecretsId, hostEnvironment?.ApplicationName, optional, reloadOnChange);
            }

            if (!optional)
            {
                throw new InvalidOperationException(string.Format(Strings.Error_Missing_UserSecretsIdAttribute, assembly.GetName().Name));
            }

            return configuration;
        }

        private static IConfigurationBuilder AddUserSecretsInternal(IConfigurationBuilder configuration, string userSecretsId, string? envApp, bool optional, bool reloadOnChange)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ = configuration ?? throw new ArgumentNullException(nameof(userSecretsId));

            if (envApp != null)
            {
                return configuration
                      .AddSecretsFile(PathHelper.InternalGetSecretsPathFromSecretsId(userSecretsId, null, throwIfNoRoot: !optional), optional, reloadOnChange)
                      .AddSecretsFile(PathHelper.InternalGetSecretsPathFromSecretsId(userSecretsId, envApp, throwIfNoRoot: !optional), optional, reloadOnChange);
            }

            return configuration.AddSecretsFile(PathHelper.InternalGetSecretsPathFromSecretsId(userSecretsId, null, throwIfNoRoot: !optional), optional, reloadOnChange);
        }

        private static IConfigurationBuilder AddSecretsFile(this IConfigurationBuilder configuration, string secretPath, bool optional, bool reloadOnChange)
        {
            if (string.IsNullOrEmpty(secretPath))
            {
                return configuration;
            }

            string? directoryPath = Path.GetDirectoryName(secretPath);
            PhysicalFileProvider? fileProvider = Directory.Exists(directoryPath)
                ? new PhysicalFileProvider(directoryPath)
                : null;
            return configuration.AddJsonFile(fileProvider, secretPath, optional, reloadOnChange);
        }
    }
}