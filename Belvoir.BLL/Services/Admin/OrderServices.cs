﻿using AutoMapper;
using Belvoir.Bll.DTO.Order;
using Belvoir.DAL.Models;
using Belvoir.DAL.Repositories.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Belvoir.Bll.Services.Admin
{
    public interface IOrderServices
    {
        public Task<Response<object>> AddTailorProducts(TailorProductDTO tailorProductDTO);
    }
    public class OrderServices:IOrderServices
    {
        private readonly IOrderRepository _repo;
        private readonly IMapper _mapper;
        public OrderServices(IOrderRepository repo,IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<Response<object>> AddTailorProducts(TailorProductDTO tailorProductDTO) {

            var tailorProduct = _mapper.Map<TailorProduct>(tailorProductDTO);
            if (!await _repo.IsClothExists(tailorProduct.ClothId))
            {
                return new Response<object> { statuscode = 404, message = "cloth not exist" };
            }
            if (!await _repo.IsDesignExists(tailorProduct.DesignId))
            {
                return new Response<object> { statuscode = 404, message = "DressDesign not exist" };
            }
            if (await _repo.AddTailorProduct(tailorProduct))
            {
                return new Response<object> { statuscode = 200, message = "success" };
            }
            return new Response<object> { statuscode = 500, message = "failed" };
        }

    }
}
