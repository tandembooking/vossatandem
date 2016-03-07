using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;
using System.Linq;
using System;

[Authorize]
public class AdminController: Controller {
    private ApplicationDbContext _context;
    
    public AdminController(ApplicationDbContext context) {
        _context = context;
    }
    
    public ActionResult Index() {
        var users = _context.Users
            .OrderBy(u => u.Name)
            .ToList();
        
        return View(users);
    } 
    
    public ActionResult SetPilot(Guid userId, bool isPilot) {
        throw new NotImplementedException();
    }
    
    public ActionResult SetAdmin(Guid userId, bool isAdmin) {
        throw new NotImplementedException();
    }
   
} 