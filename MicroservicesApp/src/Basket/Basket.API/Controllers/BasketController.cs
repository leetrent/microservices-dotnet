using AutoMapper;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using EventBusRabbitMQ.Common;
using EventBusRabbitMQ.Events;
using EventBusRabbitMQ.Producer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Basket.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly IMapper _mapper;
        private readonly EventBusRabbitMQProducer _eventBusProducer;

        public BasketController(IBasketRepository repository, IMapper mapper, EventBusRabbitMQProducer eventBusProducer)
        { 
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _eventBusProducer = eventBusProducer ?? throw new ArgumentNullException(nameof(eventBusProducer));
        }

        [HttpGet]
        [ProducesResponseType(typeof(BasketCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<BasketCart>> GetBasket(string userName)
        {
            var basket = await _repository.GetBasket(userName);
            return Ok(basket ?? new BasketCart(userName));
        }

        [HttpPost]
        [ProducesResponseType(typeof(BasketCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<BasketCart>> UpdateBasket([FromBody] BasketCart basket)
        {
            return Ok(await _repository.UpdateBasket(basket));
        }

        [HttpDelete("{userName}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBasket(string userName)
        {
            return Ok(await _repository.DeleteBasket(userName));
        }

        [Route("[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
        {
            BasketCart basketCart = await _repository.GetBasket(basketCheckout.UserName);
            if (basketCart == null)
            {
                return BadRequest();
            }

            bool deleteWasSuccessful = await _repository.DeleteBasket(basketCart.UserName);
            if (deleteWasSuccessful == false)
            {
                return BadRequest();
            }

            BasketCheckoutEvent basketCheckoutEvent = _mapper.Map<BasketCheckoutEvent>(basketCheckout);
            basketCheckoutEvent.RequestId = Guid.NewGuid();
            basketCheckoutEvent.TotalPrice = basketCart.TotalPrice;

            try
            {
                _eventBusProducer.PublishBasketCheckout(EventBusConstants.BasketCheckoutQueue, basketCheckoutEvent);
            }
            catch (Exception)
            {
                throw;
            }

            return Accepted();
        }

    }
}
