// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.22.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InterviewChatbot.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace InterviewChatbot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EmptyBot>();
            
            // Enregistrer les services de notre chatbot
            services.AddSingleton<OpenAIService>();
            services.AddSingleton<InterviewDialogService>();
            services.AddSingleton<LoggingService>();
            
            // Nouveau service d'évaluation de profil
            services.AddSingleton<UserProfileEvaluationService>();
            
            // Service d'adaptation des questions d'entretien
            services.AddSingleton<AdaptiveQuestionsService>();
            
            // MenuDialogService doit être enregistré après UserProfileEvaluationService et AdaptiveQuestionsService car il en dépend
            services.AddSingleton<MenuDialogService>();
            
            // S'assurer que la clé API OpenAI est configurée
            var openAIKey = Configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(openAIKey))
            {
                // En développement, on accepte une variable d'environnement
                openAIKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Configure les fichiers par défaut pour inclure index.html
            var defaultFileOptions = new DefaultFilesOptions();
            defaultFileOptions.DefaultFileNames.Clear();
            defaultFileOptions.DefaultFileNames.Add("index.html");
            
            app.UseDefaultFiles(defaultFileOptions)
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
