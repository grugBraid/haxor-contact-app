using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Haxor.Components;
using System.Text.Json;
using System.Text;

Database.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddAntiforgery();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseSession();

app.MapGet("/", () => Results.Redirect("/contacts"));

app.MapGet("/contacts", IResult (string q, [FromHeader(Name = "HX-Trigger")] string hxTrigger) =>
{
   var contacts = Database.Contacts;
   if (q is not null)
   {
      contacts = Database.Search(q);
      if (hxTrigger == "search")
         return new RazorComponentResult<Rows>(new { Contacts = contacts });
   }
   return new RazorComponentResult<Contacts>(new { Q = q, ContactList = contacts });
});

app.MapGet("/contacts/count", () => $"({Database.Contacts.Count()} total Contacts)");

app.MapGet("/contacts/{id}", (int id) =>
{
   var contact = Database.GetById(id);
   return new RazorComponentResult<Show>(new { Contact = contact });
});

app.MapGet("/contacts/{id}/edit", (int id) =>
{
   var contact = Database.GetById(id);
   var form = new ContactForm();
   form.Load(contact);
   return new RazorComponentResult<Edit>(new { Id = id, Form = form });
});

app.MapPost("/contacts/{id}/edit", (int id, [FromForm] ContactForm form, HttpContext context) =>
{
   var contact = Database.GetById(id);
   contact.Update(form.First, form.Last, form.Phone, form.Email);
   context.Session.SetString("FlashMessage", "Contact Updated!");
   return Results.Redirect($"/contacts/{id}");
});

app.MapGet("/contacts/{id}/email", (int id, string email) =>
{
   if (string.IsNullOrEmpty(email)) return "Email is required";
   if (!Database.IsUniqueEmail(id, email)) return "Email must be unique";
   return "";
});

app.MapGet("/contacts/new", () => new RazorComponentResult<New>(new { Form = new ContactForm() }));

app.MapPost("/contacts/new", ([FromForm] ContactForm form, HttpContext context) =>
{
   Database.Add(form.First, form.Last, form.Phone, form.Email);
   context.Session.SetString("FlashMessage", "Contact Created!");
   return Results.Redirect("/contacts");
});

app.MapGet("/contacts/{id}/delete", (int id, HttpContext context) =>
{
   Database.Delete(id);
   context.Session.SetString("FlashMessage", "Contact Deleted!");
   return Results.Redirect($"/contacts");
});

app.MapDelete("/contacts/{id}", (int id) =>
{
   Database.Delete(id);
   return Results.Ok();
});

app.MapDelete("/contacts", ([FromForm] string selected_contact_ids, HttpContext context) =>
{
   var ids = selected_contact_ids.Split(',');
   foreach (var id in ids) Database.Delete(int.Parse(id));
   context.Session.SetString("FlashMessage", $"{ids.Length} Contacts Deleted!");

   context.Response.Redirect("/contacts");
   context.Response.StatusCode = StatusCodes.Status303SeeOther;
   return Results.Empty;
});

//Archive
app.MapPost("/contacts/archive", () => {
   Archiver.Run();
   return new RazorComponentResult<Archive>();
});

app.MapGet("/contacts/archive", () => {
   return new RazorComponentResult<Archive>();
});

app.MapGet("/contacts/archive/file", (HttpContext context) => {
   var json = JsonSerializer.Serialize(Database.Contacts);
   var bytes = Encoding.ASCII.GetBytes(json);
   context.Response.Headers.Append("Context-Disposition", "attachment");
   return Results.File(bytes, "application/json", "archive.json");
});

app.MapDelete("/contacts/archive", () => {
   Archiver.Reset();
   return new RazorComponentResult<Archive>();
});

//API
app.MapGet("/api/v1/contacts", () => {
   var json = JsonSerializer.Serialize(Database.Contacts);
   return Results.Ok(json);
});

app.MapGet("/api/v1/contacts/{id:int}", (int id) => {
   var contact = Database.GetById(id);
   var json = JsonSerializer.Serialize(contact);
   return Results.Ok(json);
});

app.Run();