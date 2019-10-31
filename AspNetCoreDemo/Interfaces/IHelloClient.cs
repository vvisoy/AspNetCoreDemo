using Refit;
using System.Threading.Tasks;

namespace AspNetCoreDemo.Interfaces {
   public interface IHelloClient {
      [Get("/helloworld")]
      Task<Reply> GetMessageAsync();
   }

   public class Reply {
      public string Message { get; set; }
   }
}
