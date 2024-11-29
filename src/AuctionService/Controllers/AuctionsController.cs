using System;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController] // ApiController Attribute gicves our controller some extra abilities like data validation of required properties
[Route("api/auctions")] // Route so framework knows where to direct the HTTP request when it comes into our service
public class AuctionsController : ControllerBase // Class derives from AspNet ControllerBase class
{
    private readonly AuctionDbContext _context; // A readonly field that can be used in the rest of the class
    private readonly IMapper _mapper;

    public AuctionsController(AuctionDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions() // ActionResult lets us send back HTTP responses like 200 OK or 404 not found. Returns Type List of AuctionDto's
    {
        var auctions = await _context.Auctions
            .Include(x => x.Item) // Eagerly loads related property Item. Includes the Item for each Auction
            .OrderBy(x => x.Item.Make) // Orders the result by Car Make
            .ToListAsync();

        return _mapper.Map<List<AuctionDto>>(auctions); // Map to a list of AuctionDto and get it from auctions
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id) // Property id needs to match the property in the HttpGet line above. Return Type AuctionDto
    {
        var auction = await _context.Auctions
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (auction == null) return NotFound();

        return _mapper.Map<AuctionDto>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto) // Return Type AuctionDto
    {
        // Map the auctionDto to be created to an auction entity. Using Automapper
        var auction = _mapper.Map<Auction>(auctionDto);

        // TODO: Add current user as seller
        auction.Seller = "test"; // Placeholder till we get auth set up

        _context.Auctions.Add(auction); // Add the auction using EF. EF is effectivly tracking this in memeory. Nothing saved to DB yet

        var result = await _context.SaveChangesAsync() > 0; // Saves changes to DB. SaveChangesAsync method returns int for each change it was able to save in DB. 0 means nothing saved, result will be false. If successfull will evaluate to true.

        if (!result) return BadRequest("Could not save changes to the DB"); // Check Results

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, _mapper.Map<AuctionDto>(auction));
        // Response specifies the name of the action where the resource can be found. Want to return the ID of successfully created auction in the Header.
        // First param of CreateAtAction: GetAuctionById method we created above that requires and Id parameter
        // Second param: New {auction.Id} - the Id of newly created auction
        // Third param: return the AuctionDTO. In order to return an Auction DTO we need to go from "auction" entity into an "AuctionDto" using mapper
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto) // Not going to return anything. The client should already know what they are updating
    {
        var auction = await _context.Auctions.Include(x => x.Item) // Get the auction and include the items
            .FirstOrDefaultAsync(x => x.Id == id); // Gets the first or default auction that matches that Id

        if (auction == null) return NotFound();

        // TODO: Check seller == username. Don't have auth implemented yet

        // We want to update the properties to the updated value in the updated AuctionDto.
        // Or if not updated value not provided keep original property value of the entity
        // We will use the "??" which is the NULL conditional operator.
        // If Null or undefined we will set the Make/Model/Color etc. to what it was inside the entity.
        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
        // Since Mileage and Year are int you can't use ?? unlesss the property is optional. Had to set "?" in UpdateAuctionDto

        // Changes up to this point have been tracked in memory
        var result = await _context.SaveChangesAsync() > 0; // Saves changes to DB

        if (result) return Ok(); // 200 OK

        return BadRequest("Problem saving changes");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id) // Don't typically return anyting in a delete request. Return type is none
    {
        var auction = await _context.Auctions.FindAsync(id); // Gets the auction by id. We don't need items included at this point

        if (auction == null) return NotFound();

        // TODO: Check seller == username

        _context.Auctions.Remove(auction); // Removes the auction from memory

        var results = await _context.SaveChangesAsync() > 0; // Saves changes to DB

        if (!results) return BadRequest("Could not update DB");

        return Ok();
    }
}
