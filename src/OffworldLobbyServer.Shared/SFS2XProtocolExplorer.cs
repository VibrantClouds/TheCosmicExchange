using System.Reflection;
using Sfs2X.Requests;

namespace OffworldLobbyServer.Shared;

/// <summary>
/// Temporary class to explore SFS2X protocol constants and message types
/// </summary>
public static class SFS2XProtocolExplorer
{
    public static void ExploreMessageTypes()
    {
        Console.WriteLine("=== SFS2X Protocol Exploration ===");
        
        // Explore HandshakeRequest constants
        Console.WriteLine("\n=== HandshakeRequest Constants ===");
        ExploreTypeConstants(typeof(HandshakeRequest));
        
        // Explore LoginRequest constants
        Console.WriteLine("\n=== LoginRequest Constants ===");
        ExploreTypeConstants(typeof(LoginRequest));
        
        // Now look for Response types - this is what we need to send back!
        Console.WriteLine("\n=== Looking for Response Types ===");
        var assembly = typeof(HandshakeRequest).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace != null && type.Namespace.StartsWith("Sfs2X") && 
                type.Name.Contains("Response"))
            {
                Console.WriteLine($"Found Response type: {type.FullName}");
                ExploreTypeConstants(type);
                
                // Try to create an instance to examine properties
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance != null)
                    {
                        Console.WriteLine($"  Created {type.Name} instance:");
                        var properties = type.GetProperties();
                        foreach (var prop in properties)
                        {
                            try
                            {
                                var value = prop.GetValue(instance);
                                Console.WriteLine($"    {prop.Name} = {value} ({prop.PropertyType.Name})");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    {prop.Name} = ERROR: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Could not create {type.Name}: {ex.Message}");
                }
            }
        }
        
        // Try to find message type constants - focus on SFS2X namespace only
        Console.WriteLine("\n=== Looking for SFS2X Message Type Constants ===");
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace != null && type.Namespace.StartsWith("Sfs2X") && 
                (type.Name.Contains("Message") || type.Name.Contains("Request") || type.Name.Contains("Type") || type.Name.Contains("Event")))
            {
                Console.WriteLine($"Found SFS2X type: {type.FullName}");
                ExploreTypeConstants(type);
            }
        }
        
        // Also specifically look for the BaseRequest class which might have message type constants
        var baseRequestType = typeof(HandshakeRequest).BaseType;
        if (baseRequestType != null)
        {
            Console.WriteLine($"\n=== BaseRequest Type: {baseRequestType.FullName} ===");
            ExploreTypeConstants(baseRequestType);
        }
        
        // Try to create instances to see if we can find message type properties
        Console.WriteLine("\n=== Examining Request Instances ===");
        try
        {
            // HandshakeRequest constructor needs apiVersion, clientType, reconnectionToken
            var handshakeRequest = new HandshakeRequest("1.7.8", "Unity", null);
            Console.WriteLine($"HandshakeRequest created - examining properties...");
            var requestType = handshakeRequest.GetType();
            var properties = requestType.GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(handshakeRequest);
                    Console.WriteLine($"  {prop.Name} = {value} ({prop.PropertyType.Name})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {prop.Name} = ERROR: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not create HandshakeRequest: {ex.Message}");
        }
    }
    
    private static void ExploreTypeConstants(Type type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            try
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    var value = field.GetRawConstantValue();
                    Console.WriteLine($"  {field.Name} = {value} (const {value?.GetType().Name})");
                }
                else if (field.IsStatic)
                {
                    var value = field.GetValue(null);
                    Console.WriteLine($"  {field.Name} = {value} (static {field.FieldType.Name})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {field.Name} = ERROR: {ex.Message}");
            }
        }
    }
}