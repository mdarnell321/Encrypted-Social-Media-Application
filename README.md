# Encrypted Social Media Application
An advanced encrypted social media application that includes everything required for modern standards.\
https://www.youtube.com/watch?v=zQtWpZ1j6VM

-This project uses WPF and requires .NET Framework 4.7.2 and .NET 7.0 for the server.

### <ins>Setup Guide</ins>

__Client__\
-Modify "Network.ip" in the client to change IP address of the server (default is "127.0.0.1").\
-Modify "DatabaseCalls.host" in the client to change host of the database (default is "http://127.0.0.1"). \
-Modify "SFTPCalls.uploader" in the client to change sftp connection info.\
-Modify "SFTPCalls.www_Directory" in the client to change web root directory.

__Server__\
-Modify "_host", "_name", and "_pass" in the server to change mysql connection info.

__Database & File System__\
-Inside of the "contents" folder there is the "chatdb" file which you can import into your existing database to create the necessary tables. Next, go into your web root directory and drag in the folders and php files that are inside of the "www" folder.

### <ins>The Server</ins>
    
-Rooms have their own UDP sockets for voice chat. Calls will connect to sockets made for calls if it's under a certain user threshold, and if not, a new UDP socket is created. Everything else is sent through the server's TCP socket.

### <ins>Program Features</ins>

__Rooms__\
 &emsp;-Create and join private and public rooms that use standard encryption.\
 &emsp;-Very memory efficient method for loading old messages.\
 &emsp;-Encryption is based off the hashkey the user entered, so entering one for both creation and joining is required.\
 &emsp;-Send encrypted text, video, audio, image, and file messages. The video, audio, and images are cached so that the user doesnt have to load fresh everytime.\
 &emsp;-Search message history.\
 &emsp;-Users display in right panel. Green indicates that the user is currently in the room, blue is online, and grey is offline.\
 &emsp;-Once all users leave, the room is destroyed.

 &emsp;__Public__\
 &emsp;&emsp;-Rooms have a custom picture.\
 &emsp;&emsp;-Create voice and text channels that may allow only specific roles to access them, read-only, and have the bot send a message indicating a user has joined.\
 &emsp;&emsp;-Create new roles with special abilities and a unique display color.\
&emsp; &emsp;&emsp;-Abilities include kick, mute, ban, manage channels, and manage roles.\
 &emsp;&emsp;-Owner may not leave unless he transfers ownership.


__Direct Messaging__\
&emsp;-Asymmetrical encryption is used.\
&emsp;-Once both players remove the DM, then the entire thing deletes.\
&emsp;-Initiate a call from the DM (Once in a call, they dont have to stay in the DM window).

__Other__\
&emsp;-User profile window can display their social media.\
&emsp;-Add friends.\
&emsp;-Change profile picture.

### <ins>NuGet Packages Used</ins>

Google.Protobuf\
K4os.Compression.LZ4\
K4os.Compression.LZ4.Streams\
K4os.Hash.xxHash\
Microsoft.Win32.Registry\
Microsoft.WindowsAPICodePack-Core\
Microsoft.WindowsAPICodePack-Shell\
NAudio\
NAudio.Asio\
NAudio.Core\
NAudio.Midi\
NAudio.Wasapi\
NAudio.WinForms\
NAudio.WinMM\
NetVips\
NetVips.Native\
NetVips.Native.win-x64\
NetVips.Native.win-x86\
Portable.BouncyCastle\
SSH.NET\
System.Buffers\
System.IO.Pipelines\
System.Memory\
System.Numerics.Vectors\
System.Runtime.CompilerServices.Unsafe\
System.Security.AccessControl\
System.Security.Principal.Windows\
System.Threading.Tasks.Extensions

### [License](https://github.com/mdarnell321/Encrypted-Social-Media-Application/blob/master/LICENSE.md)


