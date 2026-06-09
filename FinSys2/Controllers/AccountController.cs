using Microsoft.AspNetCore.Mvc;
using FinSys2.Models;
using FinSys2.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text.RegularExpressions;

namespace FinSys2.Controllers
{
    public class AccountController : Controller
    {
        private readonly JsonDatabase<User> _userDb;
        private readonly IWebHostEnvironment _appEnvironment;

        public AccountController(IWebHostEnvironment appEnvironment)
        {
            _userDb = new JsonDatabase<User>("users.json", appEnvironment);
            _appEnvironment = appEnvironment;
        }

        //настройки профиля
        public IActionResult Settings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            var user = _userDb.GetAll().FirstOrDefault(u => u.Id == userId);
            return View(user);
        }

        //обновление профиля + номер
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string newName, string newPhone)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

            var users = _userDb.GetAll();
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user != null)
            {
                if (!string.IsNullOrEmpty(newPhone))
                {
                    if (!Regex.IsMatch(newPhone, @"^\d{6,14}$"))
                    {
                        return Content("<script>alert('Недопустимая длина номера телефона!'); window.location='/Account/Settings';</script>", "text/html; charset=utf-8");
                    }
                    user.Phone = newPhone;
                }

                user.FullName = newName;
                //user.Phone = newPhone;
                _userDb.SaveAll(users);

                // Перезапись куки чтобы имя в шапке обновилось сразу
                await LoginUser(user);
            }

            return RedirectToAction("Settings");
        }

        //пароль
        [HttpPost]
        public IActionResult UpdatePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = _userDb.GetAll();
            var user = users.FirstOrDefault(u => u.Id == userId);


            if (user == null) return RedirectToAction("Index", "Home");

            if (user.Password != oldPassword)
                return Content("<script>alert('Текущий пароль введен неверно!'); window.location='/Account/Settings';</script>", "text/html; charset=utf-8");

            if (newPassword != confirmPassword)
                return Content("<script>alert('Новые пароли не совпадают!'); window.location='/Account/Settings';</script>", "text/html; charset=utf-8");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
                return Content("<script>alert('Новый пароль слишком короткий!'); window.location='/Account/Settings';</script>", "text/html; charset=utf-8");

            user.Password = newPassword;
            _userDb.SaveAll(users);

            return Content("<script>alert('Пароль успешно изменен!'); window.location='/Account/Settings';</script>", "text/html; charset=utf-8");

        }

        //аватар
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile uploadedFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = _userDb.GetAll();
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user != null && uploadedFile != null)
            {
                string path = "/avatars/" + Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                string fullPath = _appEnvironment.WebRootPath + path;

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                //Сохранение
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                //Обновление данных пользователя
                user.AvatarPath = path;
                _userDb.SaveAll(users);

                //Обновление куки, чтобы аватарка обновилась в интерфейсе
                await LoginUser(user);
            }

            return RedirectToAction("Settings");
        }

        //sign in, sign up, sign out

        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (!Regex.IsMatch(model.FullName, @"^[a-zA-Zа-яА-ЯёЁ0-9 ]+$"))
                return Content("<script>alert('Имя содержит недопустимые символы!'); window.location='/';</script>", "text/html; charset=utf-8");

            if (string.IsNullOrEmpty(model.FullName) || model.FullName.Length < 2)
                return Content("<script>alert('Имя слишком короткое'); window.location='/';</script>", "text/html; charset=utf-8");

            var users = _userDb.GetAll();
            if (users.Any(u => u.Email == model.Email))
                return Content("<script>alert('Этот Email уже занят'); window.location='/';</script>", "text/html; charset=utf-8");

            model.AvatarPath = "/images/default-avatar.jpg";

            users.Add(model);
            _userDb.SaveAll(users);

            await LoginUser(model);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _userDb.GetAll().FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                await LoginUser(user);
                return RedirectToAction("Index", "Home");
            }

            return Content("<script>alert('Неверный логин или пароль'); window.location='/';</script>", "text/html; charset=utf-8");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        private async Task LoginUser(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.FullName ?? "Пользователь"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("AvatarPath", user.AvatarPath ?? "/images/BaseUser.jpg")
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity));
        }
    }
}