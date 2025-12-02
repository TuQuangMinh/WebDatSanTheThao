using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using web.Models;

namespace web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Danh sách roles có sẵn để chọn
        public List<string> AvailableRoles { get; set; } = new List<string> { "Admin", "User" };

        public class InputModel
        {
            [Required(ErrorMessage = "Full Name is required.")]
            [Display(Name = "Full Name")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Confirm Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Street Address")]
            public string StreetAddress { get; set; }

            [Display(Name = "City")]
            public string City { get; set; }

            [Display(Name = "State")]
            public string State { get; set; }

            [Display(Name = "Postal Code")]
            public string PostalCode { get; set; }

            [Required(ErrorMessage = "Role is required.")]
            [Display(Name = "Role")]
            public string Role { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            _logger.LogInformation("GET request for Register page.");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            _logger.LogInformation("POST request for Register with email: {Email}, role: {Role}",
                Input?.Email, Input?.Role);

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Input: Name={Name}, Email={Email}, Role={Role}",
                    Input.Name, Input.Email, Input.Role);

                // Kiểm tra xem role có tồn tại không
                if (!await _roleManager.RoleExistsAsync(Input.Role))
                {
                    _logger.LogWarning("Role {Role} does not exist", Input.Role);
                    ModelState.AddModelError(string.Empty, $"Role '{Input.Role}' does not exist.");
                    return Page();
                }

                // Kiểm tra xem email đã tồn tại chưa
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email is already registered.");
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    Name = Input.Name,
                    StreetAddress = Input.StreetAddress,
                    City = Input.City,
                    State = Input.State,
                    PostalCode = Input.PostalCode,
                    EmailConfirmed = true // Tự động xác nhận email
                };

                try
                {
                    _logger.LogInformation("Creating user with email: {Email}", Input.Email);
                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created successfully: {Email}", Input.Email);

                        // Thêm role cho user
                        try
                        {
                            var roleResult = await _userManager.AddToRoleAsync(user, Input.Role);
                            if (roleResult.Succeeded)
                            {
                                _logger.LogInformation("Role {Role} assigned to user: {Email}", Input.Role, Input.Email);

                                // Đăng nhập user sau khi tạo thành công
                                await _signInManager.SignInAsync(user, isPersistent: false);
                                _logger.LogInformation("User signed in: {Email}", Input.Email);

                                return LocalRedirect(returnUrl);
                            }
                            else
                            {
                                _logger.LogError("Failed to assign role {Role} to user: {Email}. Errors: {Errors}",
                                    Input.Role, Input.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                                // Xóa user nếu không thể assign role
                                await _userManager.DeleteAsync(user);

                                foreach (var error in roleResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return Page();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception assigning role {Role} to user: {Email}", Input.Role, Input.Email);

                            // Xóa user nếu có lỗi
                            try
                            {
                                await _userManager.DeleteAsync(user);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogError(deleteEx, "Failed to delete user after role assignment failure");
                            }

                            ModelState.AddModelError(string.Empty, "Error assigning role. Please try again.");
                            return Page();
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to create user: {Email}. Errors: {Errors}",
                            Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception creating user: {Email}. Message: {Message}", Input.Email, ex.Message);
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the account. Please try again.");
                    return Page();
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState invalid: {Errors}", string.Join(", ", errors));
                return Page();
            }
        }
    }
}