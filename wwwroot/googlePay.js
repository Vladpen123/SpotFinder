/**
 * Глобальные функции для работы с Google Pay API и Blazor Interop.
 */

window.onGooglePayLoaded = () => {
    console.log("Google Pay: API Script loaded.");
};

window.dotNetHelper = null;

/**
 * Инициализация моста. Теперь вызывается из дочернего компонента PaymentPanel.
 * @param {object} dotNetRef - Ссылка на экземпляр PaymentPanel.razor
 */
window.initGPay = (dotNetRef) => {
    window.dotNetHelper = dotNetRef;
    console.log("Google Pay: Мост установлен с PaymentPanel.");

    // Пытаемся отрисовать кнопку сразу, если контейнер уже в DOM
    if (window.google && window.google.payments) {
        renderGooglePayButton();
    }
};

if (typeof window.googlePayMainScriptLoaded === 'undefined') {
    window.googlePayMainScriptLoaded = true;

    const GPAY_BUTTON_CONTAINER_ID = 'gpay-container';
    const baseRequest = { apiVersion: 2, apiVersionMinor: 0 };
    const allowedCardNetworks = ["MASTERCARD", "VISA"];
    const allowedCardAuthMethods = ["PAN_ONLY", "CRYPTOGRAM_3DS"];

    const tokenizationSpecification = {
        type: 'PAYMENT_GATEWAY',
        parameters: {
            'gateway': 'example',
            'gatewayMerchantId': 'exampleGatewayMerchantId',
        },
    };

    const cardPaymentMethod = {
        type: 'CARD',
        parameters: {
            allowedAuthMethods: allowedCardAuthMethods,
            allowedCardNetworks: allowedCardNetworks,
        },
        tokenizationSpecification: tokenizationSpecification,
    };

    let paymentsClient = null;

    function getGooglePaymentsClient() {
        if (paymentsClient === null) {
            paymentsClient = new google.payments.api.PaymentsClient({
                environment: 'TEST',
                merchantInfo: {
                    merchantId: 'BCR2DN5T52P5PHLS',
                    merchantName: 'SpotFinder Service',
                },
            });
        }
        return paymentsClient;
    }

    /**
     * Рендер кнопки. Вызывается автоматически при инициализации или вручную.
     */
    window.renderGooglePayButton = function () {
        const container = document.getElementById(GPAY_BUTTON_CONTAINER_ID);
        if (!container) {
            // Если панель еще не отрендерилась, пробуем через короткий промежуток
            setTimeout(renderGooglePayButton, 100);
            return;
        }

        const client = getGooglePaymentsClient();
        const button = client.createButton({
            onClick: onGooglePaymentButtonClicked,
            buttonColor: 'black',
            buttonType: 'buy',
            buttonSizeMode: 'fill',
            buttonLocale: 'ru'
        });

        container.innerHTML = '';
        container.appendChild(button);
    };

    function onGooglePaymentButtonClicked() {
        const client = getGooglePaymentsClient();
        const paymentDataRequest = Object.assign({}, baseRequest);
        paymentDataRequest.allowedPaymentMethods = [cardPaymentMethod];
        paymentDataRequest.transactionInfo = {
            countryCode: 'UA',
            currencyCode: 'UAH',
            totalPriceStatus: 'FINAL',
            totalPrice: '15.00',
        };
        paymentDataRequest.merchantInfo = {
            merchantId: 'BCR2DN5T52P5PHLS',
            merchantName: 'SpotFinder Service',
        };

        client.loadPaymentData(paymentDataRequest)
            .then(function (paymentData) {
                const token = paymentData.paymentMethodData.tokenizationData.token;

                if (window.dotNetHelper) {
                    // Теперь вызываем метод внутри PaymentPanel.razor
                    window.dotNetHelper.invokeMethodAsync('PayWithGooglePay', token)
                        .catch(err => console.error("Ошибка вызова Blazor:", err));
                }
            })
            .catch(err => console.error("Ошибка Google Pay:", err));
    }
}