using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MemStache.Mobile.Model;
using MemStache.LiteDB;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace MemStache.Mobile
{
    public partial class App : Application
    {
        public App ()
        {
            InitializeComponent();

            StacheMeister Meister = new StacheMeister("memstache.demo", StashPlan.spSerializeCompress);
            string key = "app01_Test";

            string value = Meister[key]; //app just started, so fetching value from db cache, not memory

            //int rowcount;
            if (value != null)
            {
                //rowcount = Meister.DB.Delete<Stash>(key); //delete the record
                StashRepo.Delete(key);
            }

            Meister[key] = "This is a Test";//Assign value to MemStache
            value = Meister[key];  //will find value in memory cache, no need to check db cache

            //Now let's cache an Object
            Person user = new Person() { Name = "Dennis", Age = 44 };
            key = "App.User.Name";
            Meister[key] = user;//assign object to cache
            Person user2 = Meister[key] as Person;//the retrieved object is automatically deserialized.

            MainPage = new MainPage();
        }

        protected override void OnStart ()
        {
            // Handle when your app starts
        }

        protected override void OnSleep ()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume ()
        {
            // Handle when your app resumes
        }
    }
}
