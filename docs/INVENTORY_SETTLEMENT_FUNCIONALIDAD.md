# Funcionalidad: Inventario + Settlement

## Objetivo

Este documento explica de forma simple como funciona el flujo de inventario y como se relaciona con los reportes de settlement, manteniendo una arquitectura sencilla.

## Explicacion funcional (simple)

La aplicacion maneja dos conceptos distintos:

- **Inventario**: unidades fisicas disponibles por SKU.
- **Settlement**: corte financiero de Amazon (ventas, fees, refunds y ajustes), con un total neto depositado.

No son lo mismo:

- El inventario responde: "cuantas piezas tengo".
- El settlement responde: "cuanto dinero me liquidaron en ese periodo".

### Flujo esperado

1. Se carga inventario base por cliente (customer/vendor).
2. Se aplican actualizaciones masivas (bulk) para ajustar cantidades de SKUs.
3. Por separado, se cargan cortes de settlement para consulta historica financiera.
4. El usuario puede consultar inventario actual y, adicionalmente, revisar cada corte de settlement.

## Diseno de datos propuesto (3 tablas)

Se asume que ya existe la tabla `customer` y que contiene a tus clientes.

---

## 1) Tabla de inventario actual

### Nombre sugerido

`inventory`

### Campos

- `id` (PK)
- `customer_id` (FK -> `customer.id`)
- `sku` (varchar, requerido)
- `asin` (varchar, nullable)
- `product_name` (varchar, nullable)
- `quantity_on_hand` (int, default 0)
- `prep_owner` (varchar, nullable)  // Amazon o Seller
- `label_owner` (varchar, nullable) // Amazon o Seller
- `expiration_date` (date, nullable)
- `lot_code` (varchar, nullable)
- `units_per_box` (numeric, nullable)
- `number_of_boxes` (int, nullable)
- `box_weight_lb` (numeric, nullable)
- `created_at` (timestamp)
- `updated_at` (timestamp)

### Reglas recomendadas

- Indice unico: (`customer_id`, `sku`)

---

## 2) Tabla encabezado de settlement

### Nombre sugerido

`settlement_header`

### Campos

- `id` (PK)
- `customer_id` (FK -> `customer.id`)
- `settlement_id` (varchar, requerido)
- `settlement_start_date` (timestamp/date)
- `settlement_end_date` (timestamp/date)
- `deposit_date` (timestamp/date, nullable)
- `currency` (varchar(10))
- `total_amount` (numeric(18,2))
- `source_file_name` (varchar, nullable)
- `source_file_url` (varchar, nullable)
- `imported_at` (timestamp)
- `created_at` (timestamp)

### Reglas recomendadas

- Indice unico: (`customer_id`, `settlement_id`)

---

## 3) Tabla detalle de settlement

### Nombre sugerido

`settlement_detail`

### Campos

- `id` (PK)
- `settlement_header_id` (FK -> `settlement_header.id`)
- `transaction_type` (varchar) // Order, Refund, Fee, etc.
- `order_id` (varchar, nullable)
- `merchant_order_id` (varchar, nullable)
- `adjustment_id` (varchar, nullable)
- `shipment_id` (varchar, nullable)
- `marketplace_name` (varchar, nullable)
- `sku` (varchar, nullable)
- `quantity_purchased` (numeric, nullable)
- `amount_type` (varchar, nullable)
- `amount_description` (varchar, nullable)
- `amount` (numeric(18,2), nullable)
- `raw_row_json` (json/text, nullable)
- `created_at` (timestamp)

### Indices recomendados

- (`settlement_header_id`)
- (`order_id`)
- (`sku`)

---

## Notas finales

- Este modelo evita complejidad innecesaria.
- Separa correctamente operacion (inventario) y finanzas (settlement).
- Permite consultar inventario actual y, al mismo tiempo, mantener historico por cada corte de Amazon.
