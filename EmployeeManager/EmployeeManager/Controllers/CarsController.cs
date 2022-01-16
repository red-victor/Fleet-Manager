﻿using EmployeeManager.Data;
using EmployeeManager.Models;
using EmployeeManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EmployeeManager.DTOs;

namespace EmployeeManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _db;

        private readonly ICarService _carService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CarsController> _logger;

        public CarsController(ILogger<CarsController> logger, ApplicationDbContext db, ICarService carService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _carService = carService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<Car> AddNewCar(Car car)
        {
            _logger.LogInformation("A new car added. Id: {id}", car.Id);
            return await _carService.AddAsync(car);
        }

        [HttpGet("assigned")]
        public async Task<IEnumerable<Car>> GetAllAssigned()
        {
            _logger.LogInformation("All cars with assigned users retrieved");
            return await _carService.GetAllAssignedAsync();
        }

        [HttpGet("unassigned")]
        public async Task<IEnumerable<Car>> GetAllUnassigned()
        {
            _logger.LogInformation("All cars with no assigned users retrieved");
            return await _carService.GetAllUnassignedAsync();
        }

        [HttpGet]
        public async Task<IEnumerable<Car>> GetAll()
        {
            var cars = await _carService.GetAllAsync();
            _logger.LogInformation("All cars retrieved");
            return cars; 
        }

        [HttpGet("{id}")]
        public async Task<Car> Get(int id)
        {
            _logger.LogInformation("Car with id {Id} retrieved", id);
            return await _carService.GetAsync(id);
        }

        [HttpDelete]
        public async Task<ActionResult> Remove([FromBody] int id)
        {
            var car = await _carService.GetAsync(id);

            if (car == null)
                return NotFound();
            _logger.LogInformation("Car with id {Id} deleted", id);
            await _carService.RemoveAsync(id);
            return Ok();
        }

        [HttpPut("{carId}/assignUser")]
        public async Task<ActionResult> AssignUser([FromRoute] int carId, AssignUserDto assignUserDto)
        {
            var car = await _carService.GetAsync(carId);

            if (car.User != null)
                return BadRequest("Car already assigned");

            var user = await _userManager.FindByIdAsync(assignUserDto.UserId);

            if (user.Car == null)
                user.Car = car;
            else
                return BadRequest("User already has a Car");

            car.User = user;
            _logger.LogInformation("Car with id {IdCar} assigned to user with id {IdUser}", car.Id, user.Id);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{carId}/dissociateUser")]
        public async Task<ActionResult> DissociateUser(int carId)
        {
            var car = await _carService.GetAsync(carId);

            if (car.User == null)
                return BadRequest();
            
            var user = await _userManager.FindByIdAsync(car.UserId);
            car.User = null;
            car.UserId = null;
            user.Car = null;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Car with id {IdCar} removed from user with id {IdUser}", car.Id, user.Id);
            return Ok();
        }

        [HttpPost]
        [Route("/upload/carList")]
        public async Task<IActionResult> UploadCarsExcel(IFormFile file)
        {
            var carList = new List<Car>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                carList = Utils.ParseCarsExcel(stream);
                await _carService.AddAsync(carList);
            }
            _logger.LogInformation("Cars added from uploaded file");
            return Ok(carList);
        }
    }
}