// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.Microsoft.Extensions.Configuration.Tests;

public sealed class ConfigurationTests
{
    [Test]
    public async Task LayeredProvidersExposeSectionsAndLastProviderWins()
    {
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("{\"Service\":{\"Name\":\"json\",\"Ports\":[80,443]}}"));
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Service:Name", "memory"),
                new KeyValuePair<string, string?>("Service:Enabled", "true"),
            })
            .AddJsonStream(json)
            .AddCommandLine(new[] { "--Service:Name=command" })
            .Build();

        await Assert.That(configuration["Service:Name"]).IsEqualTo("command");
        await Assert.That(configuration["Service:Enabled"]).IsEqualTo("true");
        await Assert.That(configuration["Service:Ports:0"]).IsEqualTo("80");
        await Assert.That(configuration.GetSection("Service").Key).IsEqualTo("Service");
        await Assert.That(configuration.GetSection("Service").GetSection("Name").Value).IsEqualTo("command");
    }

    [Test]
    public async Task CommandLineMappingsAndEnvironmentTransformationAreSupported()
    {
        IConfigurationRoot commandLine = new ConfigurationBuilder()
            .AddCommandLine(new[] { "-p", "8080", "--Feature__Name=on" }, new Dictionary<string, string>
            {
                ["-p"] = "Server:Port",
            })
            .Build();

        await Assert.That(commandLine["Server:Port"]).IsEqualTo("8080");
        await Assert.That(commandLine["Feature__Name"]).IsEqualTo("on");
        const string environmentName = "NETWASM_CONFIGURATION_TEST__VALUE";
        Environment.SetEnvironmentVariable(environmentName, "environment");
        try
        {
            IConfigurationRoot environment = new ConfigurationBuilder().AddEnvironmentVariables("NETWASM_CONFIGURATION_TEST__").Build();
            await Assert.That(environment["VALUE"]).IsEqualTo("environment");
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentName, null);
        }
#if NETWASM
        await Assert.That(global::Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource.DefaultTransformation("A__B")).IsEqualTo("A:B");
        await Assert.That(global::Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource.ColonAndDotTransformation("A___B__C")).IsEqualTo("A.B:C");
#endif
    }

    [Test]
    public async Task JsonStreamRejectsNonObjectAndReloadTokenFiresForManualSet()
    {
        bool reloaded = false;
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
        bool malformed = false;
        try
        {
            _ = new ConfigurationBuilder().AddJsonStream(json).Build();
        }
        catch (FormatException)
        {
            malformed = true;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        using IDisposable registration = configuration.GetReloadToken().RegisterChangeCallback(_ => reloaded = true, null);
        configuration["Reloaded"] = "yes";
        configuration.Reload();

        await Assert.That(malformed).IsTrue();
        await Assert.That(reloaded).IsTrue();
        await Assert.That(configuration["Reloaded"]).IsEqualTo("yes");
    }

    [Test]
    public async Task GeneratedBindingCreatesNestedNullableAndCollectionValues()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Settings:Name", "netwasm"),
                new KeyValuePair<string, string?>("Settings:RetryCount", "3"),
                new KeyValuePair<string, string?>("Settings:Child:Enabled", "true"),
                new KeyValuePair<string, string?>("Settings:Tags:0", "one"),
                new KeyValuePair<string, string?>("Settings:Tags:1", "two"),
            })
            .Build();

        Settings? settings = configuration.GetSection("Settings").Get<Settings>();

        await Assert.That(settings).IsNotNull();
        await Assert.That(settings!.Name).IsEqualTo("netwasm");
        await Assert.That(settings.RetryCount).IsEqualTo(3);
        await Assert.That(settings.Child.Enabled).IsTrue();
        await Assert.That(settings.Tags.Length).IsEqualTo(2);
        await Assert.That(settings.Tags[1]).IsEqualTo("two");
    }

    [Test]
    public async Task GeneratedBindingFeedsOptionsConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Name", "configured"),
                new KeyValuePair<string, string?>("RetryCount", "7"),
            })
            .Build();
        var services = new ServiceCollection();
        services.Configure<Settings>(configuration);

        using var provider = services.BuildServiceProvider();
        Settings settings = provider.GetRequiredService<IOptions<Settings>>().Value;

        await Assert.That(settings.Name).IsEqualTo("configured");
        await Assert.That(settings.RetryCount).IsEqualTo(7);
    }

    [Test]
    public async Task GeneratedBindingPreservesValuesForMissingKeys()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Name", "updated"),
            })
            .Build();

        DefaultedSettings created = configuration.Get<DefaultedSettings>()!;
        var existing = new DefaultedSettings { RetryCount = 9 };
        configuration.Bind(existing);

        await Assert.That(created.Name).IsEqualTo("updated");
        await Assert.That(created.RetryCount).IsEqualTo(5);
        await Assert.That(created.Child.Enabled).IsTrue();
        await Assert.That(created.Tags.Length).IsEqualTo(1);
        await Assert.That(existing.Name).IsEqualTo("updated");
        await Assert.That(existing.RetryCount).IsEqualTo(9);
        await Assert.That(existing.Child.Enabled).IsTrue();
        await Assert.That(existing.Tags.Length).IsEqualTo(1);
    }

    [Test]
    public async Task GeneratedBindingClearsNullableValuesForEmptyAndExplicitNullKeys()
    {
        IConfiguration emptyValue = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("RetryCount", string.Empty),
            })
            .Build();
        IConfiguration nullValue = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("RetryCount", null),
            })
            .Build();
        IConfiguration missingValue = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var fromEmpty = new NullableDefaultSettings();
        var fromNull = new NullableDefaultSettings();
        var fromMissing = new NullableDefaultSettings();

        emptyValue.Bind(fromEmpty);
        nullValue.Bind(fromNull);
        missingValue.Bind(fromMissing);

        int? emptyWithDefault = emptyValue.GetValue<int?>("RetryCount", 5);
        int? missingWithDefault = missingValue.GetValue<int?>("RetryCount", 5);

        await Assert.That(fromEmpty.RetryCount).IsNull();
        await Assert.That(fromNull.RetryCount).IsNull();
        await Assert.That(fromMissing.RetryCount).IsEqualTo(5);
        await Assert.That(emptyWithDefault).IsNull();
        await Assert.That(missingWithDefault).IsEqualTo(5);
    }

    // Each value converts as upstream's TypeConverter for its type converts it from an invariant string.
    [Test]
    public async Task BindingConvertsValuesAsTheirTypeConvertersDo()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Mode", "fast"),
                new KeyValuePair<string, string?>("Access", "Read, Write"),
                new KeyValuePair<string, string?>("ModeNumber", "2"),
                new KeyValuePair<string, string?>("MaybeMode", string.Empty),
                new KeyValuePair<string, string?>("Started", "2026-11-14T10:20:30+02:00"),
                new KeyValuePair<string, string?>("When", "2026-11-14 10:20:30"),
                new KeyValuePair<string, string?>("Day", "2026-11-14"),
                new KeyValuePair<string, string?>("Time", "10:20:30"),
                new KeyValuePair<string, string?>("Span", "1.02:03:04"),
                new KeyValuePair<string, string?>("Id", " 6f9619ff-8b86-d011-b42d-00c04fc964ff "),
                new KeyValuePair<string, string?>("Link", "/relative/path"),
                new KeyValuePair<string, string?>("Version", "1.2.3.4"),
                new KeyValuePair<string, string?>("Letter", "x"),
                new KeyValuePair<string, string?>("Hex", "0x1F"),
                new KeyValuePair<string, string?>("Small", " -5 "),
                new KeyValuePair<string, string?>("Big", "18446744073709551615"),
                new KeyValuePair<string, string?>("Ratio", "1.5e3"),
                new KeyValuePair<string, string?>("Money", "12.75"),
                new KeyValuePair<string, string?>("Bytes", "AQID"),
                new KeyValuePair<string, string?>("Flag", " True "),
            })
            .Build();

        ValueSettings settings = configuration.Get<ValueSettings>()!;

        await Assert.That(settings.Mode).IsEqualTo(Mode.Fast);
        await Assert.That(settings.Access).IsEqualTo(Access.Read | Access.Write);
        await Assert.That(settings.ModeNumber).IsEqualTo(Mode.Fast);
        await Assert.That(settings.MaybeMode).IsNull();
        await Assert.That(settings.Started).IsEqualTo(new DateTimeOffset(2026, 11, 14, 10, 20, 30, TimeSpan.FromHours(2)));
        await Assert.That(settings.When).IsEqualTo(new DateTime(2026, 11, 14, 10, 20, 30));
        await Assert.That(settings.Day).IsEqualTo(new DateOnly(2026, 11, 14));
        await Assert.That(settings.Time).IsEqualTo(new TimeOnly(10, 20, 30));
        await Assert.That(settings.Span).IsEqualTo(new TimeSpan(1, 2, 3, 4));
        await Assert.That(settings.Id).IsEqualTo(new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"));
        await Assert.That(settings.Link!.IsAbsoluteUri).IsFalse();
        await Assert.That(settings.Link.OriginalString).IsEqualTo("/relative/path");
        await Assert.That(settings.Version).IsEqualTo(new Version(1, 2, 3, 4));
        await Assert.That(settings.Letter).IsEqualTo('x');
        await Assert.That(settings.Hex).IsEqualTo(31);
        await Assert.That(settings.Small).IsEqualTo((sbyte)-5);
        await Assert.That(settings.Big).IsEqualTo(ulong.MaxValue);
        await Assert.That(settings.Ratio).IsEqualTo(1500f);
        await Assert.That(settings.Money).IsEqualTo(12.75m);
        await Assert.That(settings.Bytes.Length).IsEqualTo(3);
        await Assert.That(settings.Bytes[2]).IsEqualTo((byte)3);
        await Assert.That(settings.Flag).IsTrue();
    }

    // A dictionary binds each child by its key, converted to the key type, into the dictionary the property holds.
    [Test]
    public async Task BindingFillsDictionariesByKey()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Limits:x", "1"),
                new KeyValuePair<string, string?>("Limits:y", "2"),
                new KeyValuePair<string, string?>("Children:first:Enabled", "true"),
                new KeyValuePair<string, string?>("Names:7", "seven"),
                new KeyValuePair<string, string?>("ByMode:Slow", "careful"),
            })
            .Build();
        var existing = new DictionarySettings();
        existing.Limits["kept"] = 9;

        configuration.Bind(existing);
        DictionarySettings created = configuration.Get<DictionarySettings>()!;

        await Assert.That(existing.Limits.Count).IsEqualTo(3);
        await Assert.That(existing.Limits["kept"]).IsEqualTo(9);
        await Assert.That(existing.Limits["y"]).IsEqualTo(2);
        await Assert.That(created.Limits.Count).IsEqualTo(2);
        await Assert.That(created.Children["first"].Enabled).IsTrue();
        await Assert.That(created.Names![7]).IsEqualTo("seven");
        await Assert.That(created.ByMode[Mode.Slow]).IsEqualTo("careful");
    }

    // GetValue converts as binding does, and fails as upstream's binder does, naming the value, its path and the type.
    [Test]
    public async Task GetValueConvertsAndReportsAValueItCannotConvert()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("App:Mode", "SLOW"),
                new KeyValuePair<string, string?>("App:MaybeMode", string.Empty),
                new KeyValuePair<string, string?>("App:Timeout", "00:00:05"),
                new KeyValuePair<string, string?>("App:Retries", "many"),
            })
            .Build();
        IConfigurationSection app = configuration.GetSection("App");

        await Assert.That(app.GetValue<Mode>("Mode")).IsEqualTo(Mode.Slow);
        await Assert.That(app.GetValue<Mode?>("MaybeMode")).IsNull();
        await Assert.That(app.GetValue<TimeSpan>("Timeout")).IsEqualTo(TimeSpan.FromSeconds(5));
        string? message = null;
        try { _ = app.GetValue<int>("Retries"); }
        catch (InvalidOperationException error) { message = error.Message; }
        await Assert.That(message).IsEqualTo("Failed to convert configuration value 'many' at 'App:Retries' to type 'System.Int32'.");
    }

    [Test]
    public async Task GeneratedBindingSupportsInheritedAndObjectCollectionProperties()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("Name", "derived"),
            new KeyValuePair<string, string?>("Children:0:Enabled", "true"),
        }).Build();
        DerivedSettings settings = configuration.Get<DerivedSettings>()!;
        await Assert.That(settings.Name).IsEqualTo("derived");
        await Assert.That(settings.Children.Count).IsEqualTo(1);
        await Assert.That(settings.Children[0].Enabled).IsTrue();
    }

    [Test]
    public async Task GeneratedBindUpdatesExistingNestedObjectWithoutDroppingSiblings()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("Child:Enabled", "false"),
        }).Build();
        var settings = new NestedDefaults();
        configuration.Bind(settings);
        await Assert.That(settings.Child.Enabled).IsFalse();
        await Assert.That(settings.Child.Label).IsEqualTo("retained");
    }

    [Test]
    public async Task ConfigurationManagerAddsProvidersWithoutReplayingStreamsOrStalingSections()
    {
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("{\"First\":\"json\"}"));
        using var manager = new ConfigurationManager();
        manager.AddJsonStream(json);
        IConfigurationSection retained = manager.GetSection("First");
        manager.Sources.Add(new global::Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
        {
            InitialData = new[] { new KeyValuePair<string, string?>("Second", "memory") },
        });
        await Assert.That(retained.Value).IsEqualTo("json");
        await Assert.That(manager["Second"]).IsEqualTo("memory");
        manager["Written"] = "yes";
        await Assert.That(manager["Written"]).IsEqualTo("yes");
    }

    [Test]
    public async Task ConfigurationManagerPublishesCoherentCollectionsBeforeReloadCallbacks()
    {
        using var manager = new ConfigurationManager();
        int expectedCount = manager.Sources.Count + 1;
        bool observedCoherentState = false;
        using IDisposable registration = ((IConfiguration)manager).GetReloadToken().RegisterChangeCallback(_ =>
        {
            observedCoherentState = manager.Sources.Count == expectedCount
                && new List<IConfigurationProvider>(((IConfigurationRoot)manager).Providers).Count == expectedCount;
            throw new InvalidOperationException("observer failure");
        }, null);

        bool callbackFailureObserved = false;
        try
        {
            manager.Sources.Add(new global::Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new[] { new KeyValuePair<string, string?>("Added", "yes") },
            });
        }
        catch (Exception)
        {
            callbackFailureObserved = true;
        }

        int providerCount = new List<IConfigurationProvider>(((IConfigurationRoot)manager).Providers).Count;
        await Assert.That(callbackFailureObserved).IsTrue();
        await Assert.That(observedCoherentState).IsTrue();
        await Assert.That(providerCount).IsEqualTo(manager.Sources.Count);
        await Assert.That(manager["Added"]).IsEqualTo("yes");

        expectedCount = manager.Sources.Count;
        observedCoherentState = false;
        using (IDisposable replaceRegistration = ((IConfiguration)manager).GetReloadToken().RegisterChangeCallback(_ =>
        {
            observedCoherentState = manager.Sources.Count == expectedCount
                && new List<IConfigurationProvider>(((IConfigurationRoot)manager).Providers).Count == expectedCount;
        }, null))
        {
            manager.Sources[1] = new global::Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource();
        }
        await Assert.That(observedCoherentState).IsTrue();

        expectedCount = manager.Sources.Count - 1;
        observedCoherentState = false;
        using (IDisposable removeRegistration = ((IConfiguration)manager).GetReloadToken().RegisterChangeCallback(_ =>
        {
            observedCoherentState = manager.Sources.Count == expectedCount
                && new List<IConfigurationProvider>(((IConfigurationRoot)manager).Providers).Count == expectedCount;
        }, null))
        {
            manager.Sources.RemoveAt(1);
        }
        await Assert.That(observedCoherentState).IsTrue();

        expectedCount = 0;
        observedCoherentState = false;
        using (IDisposable clearRegistration = ((IConfiguration)manager).GetReloadToken().RegisterChangeCallback(_ =>
        {
            observedCoherentState = manager.Sources.Count == expectedCount
                && new List<IConfigurationProvider>(((IConfigurationRoot)manager).Providers).Count == expectedCount;
        }, null))
        {
            manager.Sources.Clear();
        }
        await Assert.That(observedCoherentState).IsTrue();
    }

    [Test]
    public async Task ConfigurationReloadTokenRemainsActiveAfterRegistration()
    {
        var token = new ConfigurationReloadToken();
        using IDisposable registration = token.RegisterChangeCallback(_ => { }, null);
        await Assert.That(token.ActiveChangeCallbacks).IsTrue();
    }

#if NETWASM
    [Test]
    public async Task UnsupportedBinderOptionsFailExplicitly()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        bool rejected = false;
        try { _ = configuration.Get<Settings>(options => options.BindNonPublicProperties = true); }
        catch (NotSupportedException) { rejected = true; }
        await Assert.That(rejected).IsTrue();
    }
#endif

    public sealed class Settings
    {
        public string Name { get; set; } = string.Empty;
        public int? RetryCount { get; set; }
        public SettingsChild Child { get; set; } = new();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public sealed class SettingsChild
    {
        public bool Enabled { get; set; }
    }

    public sealed class DefaultedSettings
    {
        public string Name { get; set; } = "original";
        public int RetryCount { get; set; } = 5;
        public SettingsChild Child { get; set; } = new() { Enabled = true };
        public string[] Tags { get; set; } = new[] { "default" };
    }

    public enum Mode { None, Slow, Fast }

    [Flags]
    public enum Access { None = 0, Read = 1, Write = 2 }

    public sealed class ValueSettings
    {
        public Mode Mode { get; set; }
        public Access Access { get; set; }
        public Mode ModeNumber { get; set; }
        public Mode? MaybeMode { get; set; } = Mode.Slow;
        public DateTimeOffset Started { get; set; }
        public DateTime When { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Span { get; set; }
        public Guid Id { get; set; }
        public Uri? Link { get; set; }
        public Version? Version { get; set; }
        public char Letter { get; set; }
        public int Hex { get; set; }
        public sbyte Small { get; set; }
        public ulong Big { get; set; }
        public float Ratio { get; set; }
        public decimal Money { get; set; }
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public bool Flag { get; set; }
    }

    public sealed class DictionarySettings
    {
        public Dictionary<string, int> Limits { get; set; } = new();
        public IDictionary<string, SettingsChild> Children { get; set; } = new Dictionary<string, SettingsChild>();
        public IReadOnlyDictionary<int, string>? Names { get; set; }
        public Dictionary<Mode, string> ByMode { get; set; } = new();
    }

    public sealed class NullableDefaultSettings
    {
        public int? RetryCount { get; set; } = 5;
    }

    public class BaseSettings { public string Name { get; set; } = string.Empty; }
    public sealed class DerivedSettings : BaseSettings { public List<SettingsChild> Children { get; set; } = new(); }
    public sealed class NestedDefaults { public DetailedChild Child { get; set; } = new(); }
    public sealed class DetailedChild
    {
        public bool Enabled { get; set; } = true;
        public string Label { get; set; } = "retained";
    }
}
