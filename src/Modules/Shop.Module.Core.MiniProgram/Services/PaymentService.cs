﻿using Essensoft.AspNetCore.Payment.WeChatPay;
using Essensoft.AspNetCore.Payment.WeChatPay.Request;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shop.Module.Core.Abstractions.Data;
using Shop.Module.Core.Abstractions.Services;
using Shop.Module.Core.MiniProgram.Models;
using Shop.Module.Payments.Abstractions.Models;
using Shop.Module.Payments.Abstractions.Services;
using System;
using System.Threading.Tasks;

namespace Shop.Module.Core.MiniProgram.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IWeChatPayClient _client;
        private readonly IAppSettingService _appSettingService;

        public PaymentService(
            ILogger<PaymentService> logger,
            IWeChatPayClient client,
            IAppSettingService appSettingService)
        {
            _logger = logger;
            _client = client;
            _appSettingService = appSettingService;
        }

        public async Task<PaymentOrderBaseResponse> GeneratePaymentOrder(PaymentOrderRequest request)
        {
            var apiHost = await _appSettingService.Get(ShopKeys.ApiHost);
            var wxRequest = new WeChatPayUnifiedOrderRequest
            {
                Body = request.Subject,
                OutTradeNo = request.OrderNo,
                TotalFee = Convert.ToInt32(request.TotalAmount * 100),
                OpenId = request.OpenId,
                TradeType = "JSAPI",
                SpbillCreateIp = "127.0.0.1",
                NotifyUrl = $"{apiHost.Trim('/')}/api/mp/pay/notify/{request.OrderNo}",
            };
            var response = await _client.ExecuteAsync(wxRequest);
            if (response?.ReturnCode == "SUCCESS" && response?.ResultCode == "SUCCESS")
            {
                var req = new WeChatPayLiteAppCallPaymentRequest
                {
                    Package = "prepay_id=" + response.PrepayId
                };

                // https://pay.weixin.qq.com/wiki/doc/api/wxa/wxa_api.php?chapter=7_7&index=5
                // 将参数(parameter)给 小程序前端 让他调起支付API
                var parameter = await _client.ExecuteAsync(req);
                var json = JsonConvert.SerializeObject(parameter);
                return JsonConvert.DeserializeObject<PaymentOrderResponse>(json);
            }
            throw new Exception(response?.ReturnMsg);
        }
    }
}
