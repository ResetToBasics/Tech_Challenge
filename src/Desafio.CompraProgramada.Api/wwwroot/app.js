const output = document.getElementById("output");

function renderResult(title, status, payload) {
  output.textContent = `${title}\nStatus: ${status}\n\n${JSON.stringify(payload, null, 2)}`;
}

async function callApi(method, url, body, title) {
  try {
    const response = await fetch(url, {
      method,
      headers: {
        "Content-Type": "application/json"
      },
      body: body ? JSON.stringify(body) : undefined
    });

    const text = await response.text();
    let payload;
    try {
      payload = text ? JSON.parse(text) : {};
    } catch {
      payload = { raw: text };
    }

    renderResult(title, response.status, payload);
  } catch (error) {
    renderResult(title, "ERRO", { message: error.message });
  }
}

document.getElementById("form-cesta").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);

  const body = {
    nome: data.get("nome"),
    itens: [1, 2, 3, 4, 5].map((index) => ({
      ticker: data.get(`ticker${index}`),
      percentual: Number(data.get(`pct${index}`))
    }))
  };

  callApi("POST", "/api/admin/cesta", body, "Cadastrar/Alterar Cesta");
});

document.getElementById("form-adesao").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);

  const body = {
    nome: data.get("nome"),
    cpf: data.get("cpf"),
    email: data.get("email"),
    valorMensal: Number(data.get("valorMensal"))
  };

  callApi("POST", "/api/clientes/adesao", body, "Aderir Cliente");
});

document.getElementById("form-alterar-valor").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);
  const clienteId = Number(data.get("clienteId"));

  callApi(
    "PUT",
    `/api/clientes/${clienteId}/valor-mensal`,
    { novoValorMensal: Number(data.get("novoValorMensal")) },
    "Alterar Valor Mensal"
  );
});

document.getElementById("form-saida").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);
  const clienteId = Number(data.get("clienteId"));

  callApi("POST", `/api/clientes/${clienteId}/saida`, null, "Encerrar Adesao");
});

document.getElementById("form-executar-compra").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);

  callApi(
    "POST",
    "/api/motor/executar-compra",
    { dataReferencia: data.get("dataReferencia") },
    "Executar Compra"
  );
});

document.getElementById("form-rebalancear-desvio").addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(event.target);

  callApi(
    "POST",
    "/api/motor/rebalancear-desvio",
    {
      dataReferencia: data.get("dataReferencia"),
      limiarDesvioPontosPercentuais: Number(data.get("limiarDesvioPontosPercentuais"))
    },
    "Rebalancear por Desvio"
  );
});

document.getElementById("form-carteira").addEventListener("submit", (event) => {
  event.preventDefault();
  const clienteId = Number(new FormData(event.target).get("clienteId"));
  callApi("GET", `/api/clientes/${clienteId}/carteira`, null, "Consultar Carteira");
});

document.getElementById("form-rentabilidade").addEventListener("submit", (event) => {
  event.preventDefault();
  const clienteId = Number(new FormData(event.target).get("clienteId"));
  callApi("GET", `/api/clientes/${clienteId}/rentabilidade`, null, "Consultar Rentabilidade");
});

document.getElementById("btn-cesta-atual").addEventListener("click", () => {
  callApi("GET", "/api/admin/cesta/atual", null, "Consultar Cesta Atual");
});

document.getElementById("btn-cesta-historico").addEventListener("click", () => {
  callApi("GET", "/api/admin/cesta/historico", null, "Consultar Historico de Cestas");
});

document.getElementById("btn-custodia-master").addEventListener("click", () => {
  callApi("GET", "/api/admin/conta-master/custodia", null, "Consultar Custodia Master");
});
