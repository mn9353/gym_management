using System;

try
{
    var assembly = System.Reflection.Assembly.LoadFrom(@"GymManagementBackend\bin\Debug\net8.0\BCrypt.Net.dll");
    var type = assembly.GetType("BCrypt.Net.BCrypt");
    var method = type.GetMethod("Verify", new[] { typeof(string), typeof(string) });
    bool result = (bool)method.Invoke(null, new object[] { "9mvP@UrZaw", "$2a$11$nnagUsG1aSrhEYM097ReGeiwOLIVs6MRdvWsRrWjYZRnstK1dUDp2" });
    Console.WriteLine($"Password match: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex}");
}
