namespace Desktop.DBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

#nullable enable

record InhibitProperties
{
    public uint Version { get; set; } = default!;
}

partial class Inhibit : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Inhibit";

    public Inhibit(DesktopService service, ObjectPath path)
        : base(service, path) { }

    public Task<ObjectPath> InhibitAsync(
        string window,
        uint flags,
        Dictionary<string, VariantValue> options
    )
    {
        return this.Connection.CallMethodAsync(
            CreateMessage(),
            (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!),
            this
        );
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sua{sv}",
                member: "Inhibit"
            );
            writer.WriteString(window);
            writer.WriteUInt32(flags);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }

    public Task<ObjectPath> CreateMonitorAsync(
        string window,
        Dictionary<string, VariantValue> options
    )
    {
        return this.Connection.CallMethodAsync(
            CreateMessage(),
            (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!),
            this
        );
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "CreateMonitor"
            );
            writer.WriteString(window);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }

    public Task QueryEndResponseAsync(ObjectPath sessionHandle)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "o",
                member: "QueryEndResponse"
            );
            writer.WriteObjectPath(sessionHandle);
            return writer.CreateMessage();
        }
    }

    public ValueTask<IDisposable> WatchStateChangedAsync(
        Action<
            Exception?,
            (ObjectPath SessionHandle, Dictionary<string, VariantValue> State)
        > handler,
        bool emitOnCapturedContext = true,
        ObserverFlags flags = ObserverFlags.None
    ) =>
        base.WatchSignalAsync(
            Service.Destination,
            __Interface,
            Path,
            "StateChanged",
            (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!),
            handler,
            emitOnCapturedContext,
            flags
        );

    public Task<uint> GetVersionAsync() =>
        this.Connection.CallMethodAsync(
            CreateGetPropertyMessage(__Interface, "version"),
            (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!),
            this
        );

    public Task<InhibitProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(
            CreateGetAllPropertiesMessage(__Interface),
            (Message m, object? s) => ReadMessage(m, (DesktopObject)s!),
            this
        );
        static InhibitProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }

    public ValueTask<IDisposable> WatchPropertiesChangedAsync(
        Action<Exception?, PropertyChanges<InhibitProperties>> handler,
        bool emitOnCapturedContext = true,
        ObserverFlags flags = ObserverFlags.None
    )
    {
        return base.WatchPropertiesChangedAsync(
            __Interface,
            (Message m, object? s) => ReadMessage(m, (DesktopObject)s!),
            handler,
            emitOnCapturedContext,
            flags
        );
        static PropertyChanges<InhibitProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(),
                invalidated = new();
            return new PropertyChanges<InhibitProperties>(
                ReadProperties(ref reader, changed),
                ReadInvalidated(ref reader),
                changed.ToArray()
            );
        }
        static string[] ReadInvalidated(ref Reader reader)
        {
            List<string>? invalidated = null;
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
            while (reader.HasNext(arrayEnd))
            {
                invalidated ??= new();
                var property = reader.ReadString();
                switch (property)
                {
                    case "version":
                        invalidated.Add("Version");
                        break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }

    private static InhibitProperties ReadProperties(
        ref Reader reader,
        List<string>? changedList = null
    )
    {
        var props = new InhibitProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
                    reader.ReadSignature("u"u8);
                    props.Version = reader.ReadUInt32();
                    changedList?.Add("Version");
                    break;
                default:
                    reader.ReadVariantValue();
                    break;
            }
        }
        return props;
    }
}

partial class DesktopService
{
    public Tmds.DBus.Protocol.Connection Connection { get; }
    public string Destination { get; }

    public DesktopService(Tmds.DBus.Protocol.Connection connection, string destination) =>
        (Connection, Destination) = (connection, destination);

    public Inhibit CreateInhibit(ObjectPath path) => new Inhibit(this, path);
}

class DesktopObject
{
    public DesktopService Service { get; }
    public ObjectPath Path { get; }
    protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;

    protected DesktopObject(DesktopService service, ObjectPath path) =>
        (Service, Path) = (service, path);

    protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)
    {
        var writer = this.Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Service.Destination,
            path: Path,
            @interface: "org.freedesktop.DBus.Properties",
            signature: "ss",
            member: "Get"
        );
        writer.WriteString(@interface);
        writer.WriteString(property);
        return writer.CreateMessage();
    }

    protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)
    {
        var writer = this.Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Service.Destination,
            path: Path,
            @interface: "org.freedesktop.DBus.Properties",
            signature: "s",
            member: "GetAll"
        );
        writer.WriteString(@interface);
        return writer.CreateMessage();
    }

    protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(
        string @interface,
        MessageValueReader<PropertyChanges<TProperties>> reader,
        Action<Exception?, PropertyChanges<TProperties>> handler,
        bool emitOnCapturedContext,
        ObserverFlags flags
    )
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = Service.Destination,
            Path = Path,
            Interface = "org.freedesktop.DBus.Properties",
            Member = "PropertiesChanged",
            Arg0 = @interface,
        };
        return this.Connection.AddMatchAsync(
            rule,
            reader,
            (Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) =>
                ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
            this,
            handler,
            emitOnCapturedContext,
            flags
        );
    }

    public ValueTask<IDisposable> WatchSignalAsync<TArg>(
        string sender,
        string @interface,
        ObjectPath path,
        string signal,
        MessageValueReader<TArg> reader,
        Action<Exception?, TArg> handler,
        bool emitOnCapturedContext,
        ObserverFlags flags
    )
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = sender,
            Path = path,
            Member = signal,
            Interface = @interface,
        };
        return this.Connection.AddMatchAsync(
            rule,
            reader,
            (Exception? ex, TArg arg, object? rs, object? hs) =>
                ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
            this,
            handler,
            emitOnCapturedContext,
            flags
        );
    }

    public ValueTask<IDisposable> WatchSignalAsync(
        string sender,
        string @interface,
        ObjectPath path,
        string signal,
        Action<Exception?> handler,
        bool emitOnCapturedContext,
        ObserverFlags flags
    )
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = sender,
            Path = path,
            Member = signal,
            Interface = @interface,
        };
        return this.Connection.AddMatchAsync<object>(
            rule,
            (Message message, object? state) => null!,
            (Exception? ex, object v, object? rs, object? hs) =>
                ((Action<Exception?>)hs!).Invoke(ex),
            this,
            handler,
            emitOnCapturedContext,
            flags
        );
    }

    protected static ObjectPath ReadMessage_o(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadObjectPath();
    }

    protected static (ObjectPath, Dictionary<string, VariantValue>) ReadMessage_oaesv(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = reader.ReadDictionaryOfStringToVariantValue();
        return (arg0, arg1);
    }

    protected static uint ReadMessage_v_u(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("u"u8);
        return reader.ReadUInt32();
    }

    protected static (string, string, VariantValue[]) ReadMessage_ssav(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadString();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadArrayOfVariantValue();
        return (arg0, arg1, arg2);
    }

    protected static Dictionary<string, VariantValue> ReadMessage_v_aesv(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("a{sv}"u8);
        return reader.ReadDictionaryOfStringToVariantValue();
    }

    protected static bool ReadMessage_b(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadBool();
    }

    protected static uint ReadMessage_u(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadUInt32();
    }

    protected static Dictionary<string, VariantValue> ReadMessage_aesv(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        return reader.ReadDictionaryOfStringToVariantValue();
    }

    protected static Dictionary<string, Dictionary<string, VariantValue>> ReadMessage_aesaesv(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        return ReadType_aesaesv(ref reader);
    }

    protected static VariantValue ReadMessage_v(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadVariantValue();
    }

    protected static (string, string, VariantValue) ReadMessage_ssv(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadString();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadVariantValue();
        return (arg0, arg1, arg2);
    }

    protected static int ReadMessage_i(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadInt32();
    }

    protected static bool ReadMessage_v_b(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("b"u8);
        return reader.ReadBool();
    }

    protected static System.Runtime.InteropServices.SafeHandle ReadMessage_h(
        Message message,
        DesktopObject _
    )
    {
        var reader = message.GetBodyReader();
        return reader.ReadHandle<Microsoft.Win32.SafeHandles.SafeFileHandle>()!;
    }

    protected static byte ReadMessage_y(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadByte();
    }

    protected static int ReadMessage_v_i(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("i"u8);
        return reader.ReadInt32();
    }

    protected static long ReadMessage_v_x(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("x"u8);
        return reader.ReadInt64();
    }

    protected static (ObjectPath, string, uint) ReadMessage_osu(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadUInt32();
        return (arg0, arg1, arg2);
    }

    protected static (
        ObjectPath,
        string,
        ulong,
        Dictionary<string, VariantValue>
    ) ReadMessage_ostaesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadUInt64();
        var arg3 = reader.ReadDictionaryOfStringToVariantValue();
        return (arg0, arg1, arg2, arg3);
    }

    protected static (
        ObjectPath,
        (string, Dictionary<string, VariantValue>)[]
    ) ReadMessage_oarsaesvz(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = ReadType_arsaesvz(ref reader);
        return (arg0, arg1);
    }

    protected static string ReadMessage_s(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadString();
    }

    protected static (VariantValue, string, uint) ReadMessage_vsu(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadVariantValue();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadUInt32();
        return (arg0, arg1, arg2);
    }

    protected static string[] ReadMessage_as(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadArrayOfString();
    }

    protected static Dictionary<string, Dictionary<string, VariantValue>> ReadType_aesaesv(
        ref Reader reader
    )
    {
        Dictionary<string, Dictionary<string, VariantValue>> dictionary = new();
        ArrayEnd dictEnd = reader.ReadDictionaryStart();
        while (reader.HasNext(dictEnd))
        {
            var key = reader.ReadString();
            var value = reader.ReadDictionaryOfStringToVariantValue();
            dictionary[key] = value;
        }
        return dictionary;
    }

    protected static (string, Dictionary<string, VariantValue>)[] ReadType_arsaesvz(
        ref Reader reader
    )
    {
        List<(string, Dictionary<string, VariantValue>)> list = new();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            list.Add(ReadType_rsaesvz(ref reader));
        }
        return list.ToArray();
    }

    protected static (string, Dictionary<string, VariantValue>) ReadType_rsaesvz(ref Reader reader)
    {
        return (reader.ReadString(), reader.ReadDictionaryOfStringToVariantValue());
    }

    protected static void WriteType_aaesv(
        ref MessageWriter writer,
        Dictionary<string, VariantValue>[] value
    )
    {
        ArrayStart arrayStart = writer.WriteArrayStart(DBusType.Array);
        foreach (var item in value)
        {
            writer.WriteDictionary(item);
        }
        writer.WriteArrayEnd(arrayStart);
    }

    protected static void WriteType_arsaesvz(
        ref MessageWriter writer,
        (string, Dictionary<string, VariantValue>)[] value
    )
    {
        ArrayStart arrayStart = writer.WriteArrayStart(DBusType.Struct);
        foreach (var item in value)
        {
            WriteType_rsaesvz(ref writer, item);
        }
        writer.WriteArrayEnd(arrayStart);
    }

    protected static void WriteType_rsaesvz(
        ref MessageWriter writer,
        (string, Dictionary<string, VariantValue>) value
    )
    {
        writer.WriteStructureStart();
        writer.WriteString(value.Item1);
        writer.WriteDictionary(value.Item2);
    }
}

class PropertyChanges<TProperties>
{
    public PropertyChanges(TProperties properties, string[] invalidated, string[] changed) =>
        (Properties, Invalidated, Changed) = (properties, invalidated, changed);

    public TProperties Properties { get; }
    public string[] Invalidated { get; }
    public string[] Changed { get; }

    public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;

    public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
}
