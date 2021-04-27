using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;
using System.Threading.Tasks;
using TodoApi.Data.EfCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using ToDoAPI.PasswordHasher;

namespace TodoApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        // In Memory Data Source:
        // private readonly IInMemoryUserRepo repository;
        //
        // public UserController(IInMemoryUserRepo repository)
        // {
        //     this.repository = repository;
        // }

        // MSSQL DB
        private readonly EfCoreUserRepository repository;
        PasswordHasher passwordHasher = new PasswordHasher();
        
        public IConfiguration _configuration;

        public UserController(EfCoreUserRepository repository, IConfiguration configuration)
        {
            this.repository = repository;
            this._configuration = configuration;
        }

        [HttpGet]
        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            var allUsers = (await repository.GetAll()).Select(user => user.AsDTO());
            return allUsers;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUserAsync(Guid id)
        {
            var user = await repository.Get(id);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user.AsDTO());
        }

        [HttpPost]
        public async Task<ActionResult<UserDTO>> CreateUserAsync(CreateUserDTO userDTO)
        {
            User newUser = new()
            {
                Id = Guid.NewGuid(),
                Username = userDTO.Username,
                Email = userDTO.Email,
                Password = passwordHasher.hashPass(userDTO.Password),
                CreatedDate = DateTimeOffset.UtcNow,
                Deleted = false
            };

            await repository.Add(newUser);

            return CreatedAtAction(nameof(GetUserAsync), new { id = newUser.Id }, newUser.AsDTO());
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUserAsync(Guid id, UpdateUserDTO userDTO)
        {
            User existingUser = await repository.Get(id);

            if (existingUser is null)
            {
                return NotFound();
            }

            User updatedUser = existingUser with
            {
                Id = existingUser.Id,
                Username = existingUser.Username,
                Email = userDTO.Email,
                Password = passwordHasher.hashPass(userDTO.Password),
                CreatedDate = existingUser.CreatedDate,
                Deleted = existingUser.Deleted
            };

            await repository.Update(updatedUser);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUserAsync(Guid id)
        {
            User existingUser = await repository.Get(id);

            if (existingUser is null)
            {
                return NotFound();
            }

            await repository.Delete(id);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<ActionResult> Authenticate([FromBody] CreateUserDTO userCredentials)
        {
            IEnumerable<UserDTO> users = (await repository.GetAll()).Select(user => user.AsDTO());
            if(!users.Any(u => (u.Username == userCredentials.Username || u.Email == userCredentials.Email) && u.Password == passwordHasher.hashPass(userCredentials.Password))){
                return Unauthorized(); //or BadRequest()????
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor{
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim("Username", userCredentials.Username),
                    new Claim("Email", userCredentials.Email),
                    new Claim("Password", userCredentials.Password)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1), //depending how long we want to keep could be valid for a day
                    SigningCredentials = 
                        new SigningCredentials(
                            new SymmetricSecurityKey(tokenKey), 
                            SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            // return tokenHandler.WriteToken(token);
            if(token == null)
                return Unauthorized();
            return Ok(tokenHandler.WriteToken(token));
        }
    }
}