using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CitizenRequest.Data;
using CitizenRequest.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CitizenRequest.Controllers
{
    [Authorize(AuthenticationSchemes = "CitizenCookie")]

    public class CitizenController : Controller
    {
        private readonly CitizenRequestsContext _context;
        private readonly IWebHostEnvironment _env;

        public CitizenController(CitizenRequestsContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        //[AllowAnonymous]
        //[HttpGet]
        //public IActionResult Index()
        //{
        //    return View(new CitizenRegisterViewModel());
        //}

        
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            var model = new HomePageViewModel
            {
                Categories = categories,
                RegisterModel = new CitizenRegisterViewModel()
            };

            return View(model);
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            var categories = await _context.Categories.ToListAsync();
            var model = new HomePageViewModel
            {
                Categories = categories,
                RegisterModel = new CitizenRegisterViewModel(),
                SelectedCategoryId = TempData["SelectedCategoryId"] as int?
            };

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(CitizenRegisterViewModel registerModel, string returnUrl = null)
        {
            // Создаем составную модель, заполняя список категорий
            var model = new HomePageViewModel
            {
                RegisterModel = registerModel,
                Categories = await _context.Categories.ToListAsync()
            };

            // Проверяем валидность данных регистрации
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Проверка уникальности email и телефона
            if (await _context.Citizens.AnyAsync(c => c.Email == registerModel.Email))
            {
                ModelState.AddModelError("RegisterModel.Email", "Этот email уже зарегистрирован");
                return View("Login", model);
            }

            // Проверка уникальности телефона
            if (await _context.Citizens.AnyAsync(c => c.PhoneNumber == registerModel.PhoneNumber))
            {
                ModelState.AddModelError("RegisterModel.PhoneNumber", "Этот номер телефона уже зарегистрирован");
                return View("Login", model);
            }

            try
            {
                // Создание нового гражданина
                var citizen = new Citizen
                {
                    FullName = registerModel.FullName,
                    Email = registerModel.Email,
                    PhoneNumber = registerModel.PhoneNumber,
                    Address = registerModel.Address,
                    CreatedAt = DateTime.Now
                };

                _context.Citizens.Add(citizen);
                await _context.SaveChangesAsync();

                // Создание сессии
                var session = new CitizenSession
                {
                    CitizenId = citizen.CitizenId,
                    Token = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddDays(7),
                    IsActive = true
                };

                _context.CitizenSessions.Add(session);
                await _context.SaveChangesAsync();

                // Аутентификация
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, citizen.CitizenId.ToString()),
            new Claim(ClaimTypes.Name, citizen.FullName),
            new Claim("CitizenToken", session.Token)
        };

                var authProperties = new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    "CitizenCookie",
                    new ClaimsPrincipal(new ClaimsIdentity(claims, "CitizenCookie")),
                    authProperties);

                // Если returnUrl не пустой, перенаправляем туда.
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Dashboard", "Citizen");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}");
                ModelState.AddModelError("", "Ошибка при сохранении данных. Попробуйте снова.");
                return View("Index", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex}");
                ModelState.AddModelError("", "Произошла ошибка. Пожалуйста, попробуйте позже.");
                return View("Index", model);
            }
        }

        [HttpGet]
        public IActionResult SelectCategory(int categoryId, string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("CreateApplication", new { categoryId });
            }
            else
            {
                TempData["SelectedCategoryId"] = categoryId;
                return RedirectToAction("Login", new { returnUrl }); // Перенаправляем на регистрацию
            }
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }


        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var citizen = await GetCurrentCitizenAsync();
            if (citizen == null) return RedirectToAction("Index");

            var applications = await _context.CitizenApplications
                .Where(a => a.CitizenId == citizen.CitizenId)
                .Include(a => a.Category)
                .Include(a => a.Responses)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            return View(new DashboardViewModel
            {
                Citizen = citizen,
                Applications = applications
            });
        }


        [HttpGet]
        public async Task<IActionResult> CreateApplication(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return NotFound();

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new
                {
                    returnUrl = Url.Action("CreateApplication", new { categoryId })
                });
            }

            var model = new ApplicationCreateViewModel
            {
                SelectedCategoryId = categoryId,
                CategoryName = category.CategoryName,
                Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    }).ToListAsync()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateApplication(ApplicationCreateViewModel model)
        {
            Console.WriteLine("CreateApplication POST вызван");

            var citizen = await GetCurrentCitizenAsync();
            if (citizen == null)
            {
                Console.WriteLine("Citizen не найден");
                return RedirectToAction("Index");
            }

            if (model.SelectedCategoryId == 0)
            {
                ModelState.AddModelError("SelectedCategoryId", "Выберите категорию");
            }


            if (!ModelState.IsValid)
            {
                // Заполняем Categories только для отображения
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    }).ToListAsync();

                return View(model);
            }

            try
            {
                var application = new CitizenApplication
                {
                    CitizenId = citizen.CitizenId,
                    CategoryId = model.SelectedCategoryId,
                    Description = model.Description,
                    SubmissionDate = DateTime.Now,
                    Status = ApplicationStatus.New
                };

                _context.CitizenApplications.Add(application);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Обращение {application.ApplicationId} сохранено");
                return RedirectToAction("Dashboard", new { id = application.ApplicationId });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                ModelState.AddModelError("", "Ошибка при сохранении обращения. Попробуйте снова.");
                model.Categories = await GetCategoriesSelectListAsync();
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var citizen = await GetCurrentCitizenAsync();
            if (citizen == null) return RedirectToAction("Index");

            var application = await _context.CitizenApplications
                .Include(a => a.Category)
                .Include(a => a.Responses)
                    .ThenInclude(r => r.Admin)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.CitizenId == citizen.CitizenId);

            if (application == null) return NotFound();

            return View(new ApplicationDetailsViewModel
            {
                Application = application,
                NewMessage = new ResponseViewModel()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int applicationId, ResponseViewModel newMessage)
        {
            // Получаем текущего гражданина
            var citizen = await GetCurrentCitizenAsync();
            if (citizen == null)
            {
                // Если гражданин не найден, перенаправляем на страницу входа
                return RedirectToAction("Index");
            }

            // Проверяем, что сообщение не пустое
            if (string.IsNullOrWhiteSpace(newMessage.Message))
            {
                ModelState.AddModelError("NewMessage.Message", "Сообщение не может быть пустым.");
                // Если валидация не прошла, можно перенаправить обратно на страницу деталей
                return RedirectToAction("ApplicationDetails", new { id = applicationId });
            }

            // Создаём новую запись ответа
            var response = new Response
            {
                ApplicationId = applicationId,
                ResponseText = newMessage.Message,
                ResponseDate = DateTime.Now,
                IsFromCitizen = true,  // Сообщение отправлено гражданином
                                       // AdminId оставляем null – сообщение ещё не обработано администратором
            };

            _context.Responses.Add(response);
            await _context.SaveChangesAsync();

            // После сохранения перенаправляем обратно на страницу деталей обращения
            return RedirectToAction("ApplicationDetails", new { id = applicationId });
        }


        [HttpGet]
        public async Task<IActionResult> GetMessages(int applicationId)
        {
            var citizen = await GetCurrentCitizenAsync();
            if (citizen == null)
            {
                return Unauthorized();
            }

            var application = await _context.CitizenApplications
                .Include(a => a.Category)
                .Include(a => a.Responses)
                    .ThenInclude(r => r.Admin)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.CitizenId == citizen.CitizenId);

            if (application == null)
            {
                return NotFound();
            }

            return PartialView("_MessagesPartial", application.Responses);
        }


        private async Task<Citizen> GetCurrentCitizenAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var citizenIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (citizenIdClaim == null || !int.TryParse(citizenIdClaim.Value, out var citizenId))
                return null;

            return await _context.Citizens
                .Include(c => c.Sessions)
                .FirstOrDefaultAsync(c => c.CitizenId == citizenId);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            ViewData["Title"] = "О проекте";
            return View();
        }

        [AllowAnonymous]
        public IActionResult Help()
        {
            ViewData["Title"] = "Помощь";
            return View();
        }
        private async Task<IEnumerable<SelectListItem>> GetCategoriesSelectListAsync()
        {
            return (await _context.Categories.ToListAsync())
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CitizenCookie");
            // При необходимости удалите куки
            Response.Cookies.Delete("CitizenToken");
            return RedirectToAction("Index");
        }



    }
}
