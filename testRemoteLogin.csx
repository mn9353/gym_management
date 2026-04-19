using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

var client = new HttpClient();
var content = new StringContent("{\"identifier\":\"manoj9353780784@gmail.com\",\"password\":\"9mvP@UrZaw\"}", Encoding.UTF8, "application/json");
var response = await client.PostAsync("https://gym-management-1ekn.onrender.com/api/auth/login", content);
Console.WriteLine(response.StatusCode);
Console.WriteLine(await response.Content.ReadAsStringAsync());
