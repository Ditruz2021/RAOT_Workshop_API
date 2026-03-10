using ExampleApi.Data.DbContexts;
using ExampleApi.Dto;
using ExampleApi.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExampleApi.Services
{
    public interface IUserService
    {
        Task<PagedResult<UsersResponse>> GetPageAsync(string? keyword, int Page = 1, int PageSize = 10);
        Task<UserResponse?> GetByIdAsync(int userId);
        Task<UserResponse> CreateAsync(UserCreateRequest req);
        Task<UserResponse?> UpdateAsync(int userId, UserUpdateRequest req);
        Task<bool> DeleteAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext db;

        public UserService(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<PagedResult<UsersResponse>> GetPageAsync(string? Keyword,int Page = 1, int PageSize = 10)
        {
            Page = Math.Max(Page, 1);
            PageSize = Math.Clamp(PageSize, 1, 200);

            var BaseQuesy = from u in db.Users.AsNoTracking()
                            join ru in db.RoleUsers.AsNoTracking() on u.UserId equals ru.UserId into ruJoin
                            from ru in ruJoin.DefaultIfEmpty()
                            join r in db.Roles.AsNoTracking() on ru.RoleId equals r.RoleId into rJoin
                            from r in rJoin.DefaultIfEmpty()
                            select new { u, ru, r };


            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var k = Keyword.Trim();
                BaseQuesy = BaseQuesy.Where(x => (x.u.Username != null && x.u.Username.Contains(k)) ||
                                                 (x.u.Firstname != null && x.u.Firstname.Contains(k)) ||
                                                 (x.u.Lastname != null && x.u.Lastname.Contains(k)) ||
                                                 (x.u.Email != null && x.u.Email.Contains(k)) ||
                                                 (x.r != null && x.r.RoleName != null && x.r.RoleName.Contains(k))
                );
            }

            var Total = await BaseQuesy.CountAsync();

            var Items = await BaseQuesy
                .OrderBy(x => x.u.UserId)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new UsersResponse
                {
                    UserId = x.u.UserId,
                    Name = (x.u.Firstname ?? "") + " " + (x.u.Lastname ?? ""),
                    Username = x.u.Username,
                    Email = x.u.Email ?? "-",
                    Role = x.r != null ? x.r.RoleName : "-",
                    IsActive = x.u.IsActive
                })
                .ToListAsync();

            return new PagedResult<UsersResponse>
            {
                Page = Page,
                PageSize = PageSize,
                TotalItems = Total,
                Items = Items
            };
        }


        public async Task<UserResponse?> GetByIdAsync(int userId)
        {
            var BaseQuesy = from u in db.Users.AsNoTracking()
                            join ru in db.RoleUsers.AsNoTracking() on u.UserId equals ru.UserId
                            join r in db.Roles.AsNoTracking() on ru.RoleId equals r.RoleId
                            where u.UserId == userId
                            select new UserResponse
                            {
                                UserId = u.UserId,
                                Firstname = u.Firstname,
                                Lastname = u.Lastname,
                                Username = u.Username,
                                Email = u.Email,
                                IsActive = u.IsActive,
                                RoleId = r.RoleId,
                                RoleName = r.RoleName
                            };

            return await BaseQuesy.FirstOrDefaultAsync();
        }


        public async Task<UserResponse> CreateAsync(UserCreateRequest req)
        {
            var Username = req.Username.Trim();

            var IsDuplicate = await db.Users.AnyAsync(x => x.Username == Username);
            if (IsDuplicate) 
            {
                throw new InvalidOperationException("มีชื่อผู้ใช้ซ้ำในระบบแล้ว"); 
            }

            var Role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == req.RoleId);

            if (Role is null) 
            { 
                throw new InvalidOperationException("ไม่พบ Role ที่เลือก"); 
            }

            var user = new User
            {
                Firstname = req.Firstname.Trim(),
                Lastname = req.Lastname.Trim(),
                Username = Username,
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                IsActive = req.IsActive
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.RoleUsers.Add(new RoleUser
            {
                UserId = user.UserId,
                RoleId = Role.RoleId
            });
            await db.SaveChangesAsync();

            return new UserResponse
            {
                UserId = user.UserId,
                Firstname = user.Firstname,
                Lastname = user.Lastname,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleId = Role.RoleId,
                RoleName = Role.RoleName
            };
        }


        public async Task<UserResponse?> UpdateAsync(int UserId, UserUpdateRequest Request)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == UserId);
            if (user is null)
            { 
                return null; 
            }

            var Username = Request.Username.Trim();

            var IsDuplicate = await db.Users.AnyAsync(x => x.Username == Username && x.UserId != UserId);
            if (IsDuplicate)
            {
                throw new InvalidOperationException("มีชื่อผู้ใช้ซ้ำในระบบแล้ว");
            }

            var Role = await db.Roles.FirstOrDefaultAsync(x => x.RoleId == Request.RoleId);

            if (Role is null)
            {
                throw new InvalidOperationException("ไม่พบ Role ที่เลือก");
            }

            user.Firstname = Request.Firstname.Trim();
            user.Lastname = Request.Lastname.Trim();
            user.Username = Username;
            user.Email = string.IsNullOrWhiteSpace(Request.Email) ? null : Request.Email.Trim();
            user.IsActive = Request.IsActive;

            var ru = await db.RoleUsers.FirstOrDefaultAsync(x => x.UserId == UserId);
            if (ru is null)
            {
                db.RoleUsers.Add(new RoleUser { UserId = UserId, RoleId = Role.RoleId });
            }
            else
            {
                ru.RoleId = Role.RoleId;
            }

            await db.SaveChangesAsync();

            return new UserResponse
            {
                UserId = user.UserId,
                Firstname = user.Firstname,
                Lastname = user.Lastname,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleId = Role.RoleId,
                RoleName = Role.RoleName
            };
        }

        public async Task<bool> DeleteAsync(int UserId)
        {
            var User = await db.Users.FirstOrDefaultAsync(x => x.UserId == UserId);
            if (User is null)
            { 
                return false; 
            }

            var RoleUsers = await db.RoleUsers.Where(x => x.UserId == UserId).ToListAsync();
            if (RoleUsers.Count > 0)
            {
                db.RoleUsers.RemoveRange(RoleUsers);
            }

            db.Users.Remove(User);
            await db.SaveChangesAsync();
            return true;
        }


    }
}
