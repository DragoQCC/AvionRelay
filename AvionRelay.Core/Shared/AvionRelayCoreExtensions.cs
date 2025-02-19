using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.Core;

public static class AvionRelayCoreExtensions
{
    public static IServiceCollection AddAvionRelayCore(this IServiceCollection services)
    {
        services.AddSingleton<IRetryPolicy>();
        return services;
    }
}

public class Program
{
    public static void Main()
    {
        string person1Name = "Jon";
        Console.WriteLine($"Hello {person1Name}!");
        
        string person1Email = GetEmailFromName(person1Name);
        Console.WriteLine($"Email for {person1Name} is {person1Email}");
    }
    
    public static string GetEmailFromName(string name)
    {
        string emailEnding = "@ending.com";
        string emailFront = "";
		
        if(name.Contains(" "))
        {
            string[] splitName = name.Split(" ");
            string firstName = splitName[0];
            string lastNameChar = splitName[1].Split()[0];
            emailFront = firstName + "." + lastNameChar;
        }
        else
        {
            //emailFront = name;
            
            //let's pretend this was more complex, and we made a mistake and assigned name as null somewhere before returning emailFront
            name = null;
            emailFront = name;
        }
        return emailFront + emailEnding;
		
    }
}