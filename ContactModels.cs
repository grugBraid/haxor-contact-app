using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

public class Contact
{
   public int Id { get; }
   public string First { get; private set; }
   public string Last { get; private set; }
   public string Phone { get; private set; }
   public string Email { get; private set; }

   public Contact(int id, string first, string last, string phone, string email)
   {
      Id = id;
      First = first;
      Last = last;
      Phone = phone;
      Email = email;
   }

   public void Update(string first, string last, string phone, string email)
   {
      First = first;
      Last = last;
      Phone = phone;
      Email = email;
   }
}

public class ContactForm
{
   public string First { get; set; }
   public string Last { get; set; }
   public string Phone { get; set; }
   public string Email { get; set; }
   public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();

   public ContactForm() { }

   public void Load(Contact contact)
   {
      First = contact.First;
      Last = contact.Last;
      Phone = contact.Phone;
      Email = contact.Email;
   }
}

public static class Database
{
   public static IEnumerable<Contact> Contacts;
   private static int NextId = 6;

   public static void Load() => Contacts = GetAll();

   public static IEnumerable<Contact> GetAll()
   {
      var path = AppDomain.CurrentDomain.BaseDirectory;
      var fileName = $"{path}contacts.json";

      using var reader = File.OpenText(fileName);
      var fileContents = reader.ReadToEnd();

      return JsonSerializer.Deserialize<IEnumerable<Contact>>(fileContents);
   }

   public static Contact GetById(int id)
   {
      return Contacts.Where(c => c.Id == id).First();
   }

   public static bool IsUniqueEmail(int id, string email)
   {
      var existingContact = Contacts.Where(c => c.Id != id && String.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
      return existingContact is null;
   }

   public static IEnumerable<Contact> Search(string q)
   {
      return Contacts.Where(c => c.First.Contains(q) || c.Last.Contains(q) || c.Email.Contains(q) || c.Phone.Contains(q));
   }

   public static void Add(string first, string last, string phone, string email)
   {
      var contact = new Contact(NextId++, first, last, phone, email);
      Contacts = Contacts.Concat([contact]);
   }

   public static void Delete(int id)
   {
      var contacts = Contacts.Where(c => c.Id != id).ToList();
      Contacts = contacts;
   }
}

public static class Archiver
{
   private static string archive_status = "Waiting";
   private static float archive_progress = 0;
   private static Thread thread = null;

   public static string Status { get => archive_status; }
   public static float Progress { get => archive_progress; }

   public static void Run()
   {
      if (archive_status == "Waiting")
      {
         archive_status = "Running";
         archive_progress = 0;
         thread = new Thread(Archive);
         thread.Start();
      }
   }

   public static void Reset()
   {
      archive_status = "Waiting";
   }

   private static void Archive()
   {
      var random = new Random();
      foreach (var i in Enumerable.Range(0, 10))
      {
         Thread.Sleep(random.Next(1, 1000));
         if (archive_status != "Running") return;
         archive_progress = (i + 1) / 10f;
         Console.WriteLine($"Here... {archive_progress}");
      }
      Thread.Sleep(1);
      if (archive_status != "Running") return;
      archive_status = "Complete";
   }
}