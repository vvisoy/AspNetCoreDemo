using AspNetCoreDemo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AspNetCoreDemo.Controllers {
   public class ValuesController : ControllerBase {
      
      private readonly IHelloClient _client;

      public ValuesController(IHelloClient client) {
         _client = client;
      }

      [HttpGet("/")]
      public async Task<ActionResult<Reply>> Index() {
         return await _client.GetMessageAsync();
      }
   }
}
